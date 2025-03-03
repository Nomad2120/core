using OSI.Core.Models.Db;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using OSI.Core.Models.Enums;
using System.Diagnostics;

namespace OSI.Core.Logic
{
    public static class FineLogic
    {
        public static async Task CreateFine(int osiId, int year, int month, decimal baseRate)
        {
            //Stopwatch sw = Stopwatch.StartNew();
            using var db = OSIBillingDbContext.DbContext;
            Osi osi = await db.Osies.FirstOrDefaultAsync(a => a.Id == osiId)
                ?? throw new Exception("ОСИ не найден");

            var fineMonth = new DateTime(year, month, 1);

            if (fineMonth > DateTime.Today.AddDays(1 - DateTime.Today.Day)) throw new Exception("Нельзя начислить пеню на будущее время");

            var firstOsiTransactionDt = await db.Transactions
               .Where(t => t.OsiId == osiId && (t.TransactionType == TransactionTypeCodes.ACC || t.TransactionType == TransactionTypeCodes.PAY))
               .OrderBy(t => t.Dt)
               .Select(t => t.Dt)
               .FirstOrDefaultAsync();
            var dateBegin =
                firstOsiTransactionDt == default ?
                throw new Exception("Невозможно определить месяц включения ОСИ") :
                new DateTime(firstOsiTransactionDt.Date.Year, firstOsiTransactionDt.Date.Month, 1);

            if (fineMonth < dateBegin) throw new Exception("Указана дата раньше даты включения ОСИ");

            var fineMultiplier = baseRate / 100m / 365m;

            // проверим начислялась ли уже пеня на этот месяц
            if (await db.Transactions.AnyAsync(t => t.Dt >= fineMonth && t.TransactionType == TransactionTypeCodes.FINE && t.OsiId == osiId))
            {
                throw new Exception($"Пеня по {osi.Name} за {fineMonth:MMMM yyyy} или позже уже начислена");
            }

            var prevMonth = fineMonth.AddMonths(-1);

            var abonents = await db.Abonents.Where(a => a.OsiId == osiId).ToListAsync();

            var dbTransactions = await db.Transactions
                .Where(t => t.Dt >= prevMonth
                         && t.Dt < fineMonth
                         && t.OsiId == osiId
                         && t.GroupId != 3) // OSI-351 начислять пеню на Разовый целевой взнос (Id 3) нельзя
                .GroupBy(t => new { t.Dt.Date, t.AbonentId, t.GroupId })
                .Select(g => new
                {
                    g.Key.Date,
                    g.Key.AbonentId,
                    g.Key.GroupId,
                    AccAmount = g.Where(t => t.TransactionType == TransactionTypeCodes.ACC || (t.TransactionType == TransactionTypeCodes.FIX && t.Amount > 0)).Sum(t => t.Amount),
                    PayAmount = g.Where(t => t.TransactionType == TransactionTypeCodes.PAY).Sum(t => t.Amount),
                    FixAmount = g.Where(t => t.TransactionType == TransactionTypeCodes.FIX && t.Amount < 0).Sum(t => t.Amount),
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var unpaidFines = await db.Fines
                .AsTracking()
                .Include(f => f.Transaction)
                .Where(f => f.Transaction.Dt < fineMonth
                         && f.Transaction.OsiId == osiId
                         && f.UnpaidAmount > 0)
                .ToListAsync();

            //sw.Stop();
            //var preparationTime = sw.Elapsed;
            //Debug.WriteLine($"Preparation time: {sw.Elapsed}");

            //var loopTime = TimeSpan.Zero;

            // бежим по абонентам ОСИ
            foreach (var abonent in abonents)
            {
                //var abonentTime = TimeSpan.Zero;
                //Debug.WriteLine($"AbonentId {abonent.Id}");
                //sw.Restart();
                // конечное сальдо на конец предпредыдущего месяца без учета неоплаченной пени
                var endSaldo = await OSVLogic.GetEndSaldoOnDateByAbonent(prevMonth.AddDays(-1), abonent, debtIncludeUnpaidFine: false);
                //sw.Stop();
                //abonentTime += sw.Elapsed;
                //Debug.WriteLine($"End saldo time: {sw.Elapsed}");
                //Debug.WriteLine($"End saldo:\n{string.Join('\n', endSaldo.Services.Select(s => s.ServiceName + ' ' + s.Debt))}");

                // бежим по группам услуг с долгом // OSI-351 начислять пеню на Разовый целевой взнос (Id 3) нельзя
                foreach (var service in endSaldo.Services.Where(s => s.Debt > 0 && s.ServiceId != 3))
                {
                    //sw.Restart();

                    // Получим начисления, оплаты и корректировки за предыдущий месяц по дням
                    var days = dbTransactions
                        .Where(t => t.AbonentId == abonent.Id
                                 && t.GroupId == service.ServiceId
                                 && (t.AccAmount != 0 || t.PayAmount != 0 || t.FixAmount != 0))
                        .Select(t => new
                        {
                            t.Date,
                            t.AccAmount,
                            t.PayAmount,
                            t.FixAmount,
                        })
                        .OrderBy(x => x.Date)
                        .ToList();
                    days.Add(new
                    {
                        Date = fineMonth,
                        AccAmount = 0m,
                        PayAmount = 0m,
                        FixAmount = 0m
                    });

                    // Расчет пени
                    var debt = service.Debt;
                    var accuralsAmount = 0m;
                    var fine = 0m;
                    var fineStartDate = prevMonth;

                    foreach (var day in days)
                    {
                        accuralsAmount += day.AccAmount; // увеличиваем сумму начислений

                        if (debt > 0) // если долг > 0 вычисляем пеню за прошедшие дни
                        {
                            fine += Math.Round(debt * (day.Date - fineStartDate).Days * fineMultiplier, 2, MidpointRounding.AwayFromZero);
                            fineStartDate = day.Date;
                        }

                        debt += day.FixAmount; // уменьшаем долг отрицательными корректировками, они могут влиять на сумму начислений
                        if (debt < 0) // если долг < 0 значит долг погашен, теперь гасятся начисления
                        {
                            accuralsAmount += debt; // даже если сумма начислений уйдет в минус, гасим будущие начисления/положительные корректировки
                            debt = 0;
                        }

                        debt += day.PayAmount; // уменьшаем долг оплатами
                        if (debt < 0) // если долг < 0 значит долг погашен, теперь сначала гасятся начисления, потом пеня
                        {
                            if (accuralsAmount > 0)
                            {
                                var minAmount = Math.Min(-debt, accuralsAmount); // выбираем наименьшее по модулю, и прибавляем его к долгу и отнимаем от начислений
                                debt += minAmount;                               // например -300 и 800, берем 300, получаем 0 и 500
                                accuralsAmount -= minAmount;                     // например -300 и 100, берем 100, получаем -200 и 0
                            }

                            while (debt < 0) // отметим оплаченными старые неоплаченные пени
                            {
                                var unpaidFine = unpaidFines
                                    .OrderBy(f => f.Transaction.Dt)
                                    .FirstOrDefault(f => f.Transaction.AbonentId == abonent.Id
                                                      && f.Transaction.GroupId == service.ServiceId
                                                      && f.UnpaidAmount > 0);
                                if (unpaidFine == null) break;

                                var minAmount = Math.Min(-debt, unpaidFine.UnpaidAmount); // выбираем наименьшее по модулю, и прибавляем его к долгу и отнимаем от начислений
                                debt += minAmount;                                        // например -300 и 800, берем 300, получаем 0 и 500
                                unpaidFine.UnpaidAmount -= minAmount;                     // например -300 и 100, берем 100, получаем -200 и 0
                            }

                            if (debt < 0) // если тут долг < 0 значит все пени погашены, можно досрочно завершить работу
                            {
                                break;
                            }
                        }
                    }

                    if (fine > 0)
                    {
                        // Создаем транзакцию и запись с неоплаченной суммой
                        db.Transactions.Add(new()
                        {
                            AbonentId = abonent.Id,
                            Amount = fine,
                            Dt = fineMonth,
                            GroupId = service.ServiceId,
                            OsiId = osiId,
                            TransactionType = TransactionTypeCodes.FINE,
                            Fine = new()
                            {
                                UnpaidAmount = fine,
                            }
                        });
                    }
                    //sw.Stop();
                    //abonentTime += sw.Elapsed;
                    //Debug.WriteLine($"{service.ServiceName} time: {sw.Elapsed}");
                    //Debug.WriteLine($"{service.ServiceName} fine: {fine}");
                }
                //loopTime += abonentTime;
                //Debug.WriteLine($"AbonentId {abonent.Id} time: {abonentTime}");
            }
            //Debug.WriteLine($"Loop time: {loopTime}");

            //sw.Restart();
            await db.SaveChangesAsync();
            //sw.Stop();
            //Debug.WriteLine($"SaveChanges time: {sw.Elapsed}");
            //Debug.WriteLine($"Total time: {preparationTime + loopTime + sw.Elapsed}");
        }

        public static async Task<IEnumerable<int>> GetOsiIdsToCreateFine()
        {
            using var db = OSIBillingDbContext.DbContext;
            return await db.Osies.Where(o => o.IsLaunched && o.CreateFine && o.PlanAccurals.Count(pa => pa.AccuralCompleted) >= 2).Select(o => o.Id).ToListAsync();
        }
    }
}
