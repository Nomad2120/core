using ESoft.CommonLibrary;
using Microsoft.EntityFrameworkCore;
using OSI.Core.Helpers;
using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using OSI.Core.Models.Reports;
using OSI.Core.Models.Reports.SaldoOnAllPeriod;
using OSI.Core.Models.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OSI.Core.Logic
{
    public static class OSVLogic
    {
        public static async Task<EndSaldoResponse> GetEndSaldoOnDateByAbonent(DateTime onDate, Abonent abonent, bool debtIncludeUnpaidFine = true)
        {
            using var db = OSIBillingDbContext.DbContext;
            List<EndSaldoService> services = await db.Transactions
                .Where(t => t.Dt < onDate.Date.AddDays(1) && t.AbonentId == abonent.Id && t.Amount != 0)
                .Select(t => new { Group = new { t.Group.Id, t.Group.NameRu }, t.Amount, Fine = new { t.Fine.UnpaidAmount } })
                .GroupBy(x => new { x.Group.Id, x.Group.NameRu })
                .Select(g => new EndSaldoService
                {
                    ServiceId = g.Key.Id,
                    ServiceName = g.Key.NameRu,
                    Debt = Math.Round(
                        debtIncludeUnpaidFine
                        ? g.Sum(x => x.Amount)
                        : g.Sum(x => x.Amount) - g.Sum(x => x.Fine.UnpaidAmount),
                        2),
                })
                .ToListAsync();

            var response = new EndSaldoResponse
            {
                Services = services,
                TotalDebt = services.Sum(s => s.Debt)
            };

            return response;
        }

        public static async Task<EndSaldoResponse> GetActiveEndSaldoOnDateByAbonent(DateTime onDate, Abonent abonent)
        {
            var response = await GetEndSaldoOnDateByAbonent(onDate, abonent);

            using var db = OSIBillingDbContext.DbContext;

            var servicesToExclude = new List<EndSaldoService>();
            foreach (var service in response.Services)
            {
                var connectedServices = await db.ConnectedServices
                    .Include(cs => cs.OsiService)
                    .Where(cs => cs.AbonentId == abonent.Id
                        && cs.OsiService.ServiceGroupId == service.ServiceId
                        && cs.OsiService.IsActive
                        && cs.Dt < onDate.Date.AddDays(1))
                    .ToListAsync();

                if (!connectedServices.Any() || connectedServices
                    .GroupBy(cs => cs.OsiServiceId)
                    .All(g => g
                        .OrderByDescending(cs => cs.Dt)
                        .FirstOrDefault()
                        ?.IsActive != true))
                {
                    if (service.Debt <= 0) servicesToExclude.Add(service);
                }
            }
            foreach (var service in servicesToExclude)
            {
                response.Services.Remove(service);
            }

            response.TotalDebt = response.Services.Sum(s => s.Debt);

            return response;
        }

        public static async Task<IEnumerable<SaldoPeriod>> GetEndSaldoOnAllPeriodByAbonent(Abonent abonent)
        {
            //(string Period, string PeriodDescription) GetPeriodDescription(int month)
            //{
            //    int year = month / 12;
            //    string period = year.ToString();
            //    int m = month % 12 == 0 ? 12 : month % 12;
            //    period += m.ToString("00"); 
            //    string periodDescription = m switch
            //    {
            //        1 => "Январь",
            //        2 => "Февраль",
            //        3 => "Март",
            //        4 => "Апрель",
            //        5 => "Май",
            //        6 => "Июнь",
            //        7 => "Июль",
            //        8 => "Август",
            //        9 => "Сентябрь",
            //        10 => "Октябрь",
            //        11 => "Ноябрь",
            //        12 => "Декабрь",
            //        _ => ""
            //    } + " " + year.ToString();
            //    return (period, periodDescription);
            //}

            // !!! Предлагаю вывести в отдельный класс
            //ToTitleCase для перевода первой буквы в верхний регистр, т.к. в разных языках и/или фреймворках(.Net Framework, .Net Core) названия месяцев то с большой то с маленькой буквы начинаются
            static string GetPeriodDescription(DateTime dt)
            {
                CultureInfo cultureInfo = CultureInfo.GetCultureInfo("ru-RU");
                return cultureInfo.TextInfo.ToTitleCase(dt.ToString("MMMM yyyy", cultureInfo.DateTimeFormat));
            }

            using var db = OSIBillingDbContext.DbContext;
            // OSI-144 не берем первый проведенный план начислений
            // платежи могут быть сделаны до первого плана начислений, поэтому смотрим на транзакции
            var firstOsiTransactionDt = await db
                .Transactions
                .Where(t => t.OsiId == abonent.OsiId && (t.TransactionType == TransactionTypeCodes.ACC || t.TransactionType == TransactionTypeCodes.PAY))
                .OrderBy(t => t.Dt)
                .Select(t => t.Dt)
                .FirstOrDefaultAsync();
            var dateBegin =
                firstOsiTransactionDt == default ?
                throw new Exception("Невозможно определить месяц включения ОСИ") :
                new DateTime(firstOsiTransactionDt.Date.Year, firstOsiTransactionDt.Date.Month, 1);

            var dateEnd = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var groupedTransactions = await db.Transactions
                .Include(t => t.Group)
                .Include(t => t.Fine)
                .Where(t => t.AbonentId == abonent.Id && t.Amount != 0)
                .Select(t => new { t.Dt, t.TransactionType, t.GroupId, Group = new { t.Group.NameRu, t.Group.NameKz, }, t.Amount, Fine = new { t.Fine.UnpaidAmount } })
                .GroupBy(t => new { Period = new DateTime(t.Dt.Year, t.Dt.Month, 1), t.TransactionType, t.GroupId, t.Group.NameRu, t.Group.NameKz })
                .Select(g => new
                {
                    Period = g.Key.Period,
                    GroupId = g.Key.GroupId,
                    ServiceName = g.Key.NameRu,
                    ServiceNameKz = g.Key.NameKz,
                    Type = g.Key.TransactionType,
                    // +++
                    // 12-01-2023, shuma, по замечаниям в слаке
                    // https://grafltd.slack.com/archives/G01L2C71PP1/p1673434956300259                    
                    AmountDebet = Math.Round(g.Where(t => t.Amount >= 0).Sum(t => t.Amount), 2),
                    SumOfAccurals = Math.Round(g.Where(t => t.TransactionType == TransactionTypeCodes.ACC).Sum(t => t.Amount), 2),
                    DebetWithoutFixes = Math.Round(g.Where(t => t.TransactionType.In(TransactionTypeCodes.ACC, TransactionTypeCodes.FINE)).Sum(t => t.Amount), 2),
                    SumOfFixes = Math.Round(g.Where(t => t.TransactionType == TransactionTypeCodes.FIX).Sum(t => t.Amount), 2),
                    HasFixes = g.Count(t => t.TransactionType == TransactionTypeCodes.FIX) > 0, //OSI-270
                    SumOfFines = Math.Round(g.Where(t => t.TransactionType == TransactionTypeCodes.FINE).Sum(t => t.Amount), 2), // OSI-313
                    SumOfUnpaidFines = Math.Round(g.Where(t => t.TransactionType == TransactionTypeCodes.FINE).Sum(t => t.Fine.UnpaidAmount), 2), // OSI-313
                    //AmountKredit = Math.Round(g.Where(t => t.TransactionType == TransactionTypeCodes.PAY).Sum(t => t.Amount) * -1, 2)
                    // 13-01-2023, shuma, должны считаться сальдо на начало
                    AmountKredit = Math.Round(g.Where(t => t.TransactionType != TransactionTypeCodes.FIX && t.Amount < 0).Sum(t => t.Amount) * -1, 2)
                    // ---
                })
                .ToListAsync();

            var result = new List<SaldoPeriod>();
            DateTime period = dateBegin;
            SaldoPeriod prevSaldoPeriod = null;
            bool initialSaldoAdded = false;
            while (period <= dateEnd)
            {
                SaldoPeriod saldoPeriod = new SaldoPeriod
                {
                    Period = period,
                    PeriodDescription = GetPeriodDescription(period),
                    Services = new List<SaldoPeriodItem>(),
                };

                // добавим сальдо на начало в самый первый период
                if (!initialSaldoAdded)
                {
                    if (groupedTransactions.Any(x => x.Type == TransactionTypeCodes.SALDO))
                    {
                        saldoPeriod.Services.AddRange(groupedTransactions.Where(x => x.Type == TransactionTypeCodes.SALDO).Select(x => new SaldoPeriodItem
                        {
                            ServiceName = x.ServiceName,
                            ServiceNameKz = x.ServiceNameKz,
                            Begin = x.AmountDebet == 0 ? x.AmountKredit * -1 : x.AmountDebet
                        }));
                    }
                    initialSaldoAdded = true;
                }
                else
                {
                    // добавляем в новом периоде услуги предыдущего периода с переносом суммы на начало
                    saldoPeriod.Services.AddRange(prevSaldoPeriod.Services.Select(x => new SaldoPeriodItem
                    {
                        ServiceName = x.ServiceName,
                        ServiceNameKz = x.ServiceNameKz,
                        Begin = x.End
                    }));
                }

                // есть ли транзакции за данный период, то укажем их в дебете или кредите текущего периода
                var periodTransactions = groupedTransactions.Where(x => x.Period == saldoPeriod.Period).ToList();
                if (periodTransactions.Any())
                {
                    foreach (var pt in periodTransactions)
                    {
                        // добавляем эту услугу в период, если до сих пор нет
                        var service = saldoPeriod.Services.FirstOrDefault(x => x.ServiceName == pt.ServiceName);
                        if (service == null)
                        {
                            service = new SaldoPeriodItem
                            {
                                ServiceName = pt.ServiceName,
                                ServiceNameKz = pt.ServiceNameKz
                            };
                            saldoPeriod.Services.Add(service);
                        }
                        service.Debet += pt.AmountDebet;
                        service.Kredit += pt.AmountKredit;
                        service.DebetWithoutFixes += pt.DebetWithoutFixes;
                        service.SumOfFixes += pt.SumOfFixes;
                        service.SumOfAccurals += pt.SumOfAccurals;
                        service.SumOfFines += pt.SumOfFines;
                        service.SumOfUnpaidFines += pt.SumOfUnpaidFines;

                        //OSI-270
                        if (pt.HasFixes)
                        {
                            service.Fixes = await db.Transactions
                                .Include(t => t.Fix)
                                .Where(t => t.Dt >= period && t.Dt < period.AddMonths(1) &&
                                            t.AbonentId == abonent.Id &&
                                            t.GroupId == pt.GroupId &&
                                            t.TransactionType == TransactionTypeCodes.FIX)
                                .Select(t => new SaldoPeriodItemFix
                                {
                                    Dt = t.Dt,
                                    Reason = t.Fix.Reason,
                                    Amount = t.Amount,
                                })
                                .ToListAsync();
                        }
                    }
                }

                // считаем сальдо на конец по всем услугам текущего периода
                saldoPeriod.Services.ForEach((x) =>
                {
                    x.End = x.Begin + x.DebetWithoutFixes + x.SumOfFixes - x.Kredit;
                });

                result.Add(saldoPeriod);
                prevSaldoPeriod = saldoPeriod;
                period = period.AddMonths(1);
            }

            return result;
        }

        private static OSV CreateOSV(IEnumerable<Abonent> abonents)
        {
            OSV response = new OSV
            {
                Abonents = new List<OSVAbonent>()
            };
            response.Abonents.AddRange(abonents.Select(a => new OSVAbonent
            {
                AbonentId = a.Id,
                AbonentName = a.Name,
                IsActive = a.IsActive,
                ErcAccount = a.ErcAccount,
                AreaTypeCode = a.AreaTypeCode,
                Flat = a.External ? $"Аренда({a.Name})" : a.Flat,
                Owner = a.Owner,
                ServicesSaldo = new Dictionary<string, OSVSaldo>()
            }));
            return response;
        }

        private static async Task FillOSVOnPeriod(DateTime onDate1, DateTime onDate2, OSV response,
            Expression<Func<Transaction, bool>> predicate, bool forDebtors)
        {
            using var db = OSIBillingDbContext.DbContext;

            // начальное сальдо - берем сумму транзакций на начало периода
            (await db.Transactions
                .Include(t => t.Group)
                .Include(t => t.Abonent)
                .Where(t => t.Dt < onDate1.Date && t.Amount != 0)
                .Where(predicate)
                .GroupBy(t => new { t.Abonent.Id, t.Abonent.AreaTypeCode, t.GroupId, t.Group.NameRu, t.Group.NameKz })
                .Select(g => new
                {
                    AbonentId = g.Key.Id,
                    AreaTypeCode = g.Key.AreaTypeCode,
                    ServiceGroupId = g.Key.GroupId,
                    ServiceName = g.Key.NameRu,
                    ServiceNameKz = g.Key.NameKz,
                    Amount = Math.Round(g.Sum(t => t.Amount), 2)
                })
                .ToListAsync())
                .ForEach((a) =>
                {
                    var osvAbonent = response.Abonents.FirstOrDefault(ab => ab.AbonentId == a.AbonentId);
                    if (osvAbonent != null)
                    {
                        if (!osvAbonent.ServicesSaldo.ContainsKey(a.ServiceName) || osvAbonent.ServicesSaldo[a.ServiceName] == null)
                        {
                            osvAbonent.ServicesSaldo[a.ServiceName] = new OSVSaldo() { ServiceGroupId = a.ServiceGroupId, ServiceNameKz = a.ServiceNameKz };
                        }
                        osvAbonent.ServicesSaldo[a.ServiceName].Begin = a.Amount; // начальное
                        osvAbonent.ServicesSaldo[a.ServiceName].End = a.Amount; // конечное
                    }
                });

            // дебетовые и кредитовые обороты - берем суммы транзакций с начала по конец периода
            (await db.Transactions
                .Include(t => t.Group)
                .Include(t => t.Abonent)
                .Where(t => t.Dt >= onDate1.Date && t.Dt < onDate2.Date.AddDays(1))
                .Where(predicate)
                .GroupBy(t => new { t.Abonent.Id, t.Abonent.AreaTypeCode, t.GroupId, t.Group.NameRu, t.Group.NameKz })
                .Select(g => new
                {
                    AbonentId = g.Key.Id,
                    AreaTypeCode = g.Key.AreaTypeCode,
                    ServiceGroupId = g.Key.GroupId,
                    ServiceName = g.Key.NameRu,
                    ServiceNameKz = g.Key.NameKz,
                    // +++
                    // 12-01-2023, shuma, по замечаниям в слаке
                    // https://grafltd.slack.com/archives/G01L2C71PP1/p1673434956300259
                    Debet = Math.Round(
                        g.Where(t =>
                            t.TransactionType.In(TransactionTypeCodes.ACC, TransactionTypeCodes.FIX, TransactionTypeCodes.FINE)
                            && t.Amount >= 0)
                        .Sum(t => t.Amount), 2),
                    DebetWithoutFixes = Math.Round(
                        g.Where(t => t.TransactionType.In(TransactionTypeCodes.ACC, TransactionTypeCodes.FINE))
                        .Sum(t => t.Amount), 2),
                    SumOfAccurals = Math.Round(
                        g.Where(t => t.TransactionType == TransactionTypeCodes.ACC)
                        .Sum(t => t.Amount), 2),
                    CurrentMonthAccurals = Math.Round(
                        g.Where(t => t.TransactionType == TransactionTypeCodes.ACC
                            && t.Dt >= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1))
                        .Sum(t => t.Amount), 2),
                    SumOfFixes = Math.Round(g.Where(t => t.TransactionType == TransactionTypeCodes.FIX).Sum(t => t.Amount), 2),
                    SumOfFines = Math.Round(g.Where(t => t.TransactionType == TransactionTypeCodes.FINE).Sum(t => t.Amount), 2),
                    Kredit = Math.Round(g.Where(t => t.TransactionType == TransactionTypeCodes.PAY).Sum(t => t.Amount), 2),
                    // ---
                })
                .ToListAsync())
                .ForEach((a) =>
                {
                    var osvAbonent = response.Abonents.FirstOrDefault(ab => ab.AbonentId == a.AbonentId);
                    if (osvAbonent != null)
                    {
                        if (!osvAbonent.ServicesSaldo.ContainsKey(a.ServiceName) || osvAbonent.ServicesSaldo[a.ServiceName] == null)
                        {
                            osvAbonent.ServicesSaldo[a.ServiceName] = new OSVSaldo() { ServiceGroupId = a.ServiceGroupId, ServiceNameKz = a.ServiceNameKz };
                        }
                        osvAbonent.ServicesSaldo[a.ServiceName].Debet = a.Debet; // дебет
                        osvAbonent.ServicesSaldo[a.ServiceName].DebetWithoutFixes = a.DebetWithoutFixes; // дебет без корректировок // OSI-190
                        osvAbonent.ServicesSaldo[a.ServiceName].SumOfFixes = a.SumOfFixes; // сумма корректировок // OSI-190
                        osvAbonent.ServicesSaldo[a.ServiceName].SumOfAccurals = a.SumOfAccurals; // сумма начислений
                        osvAbonent.ServicesSaldo[a.ServiceName].SumOfFines = a.SumOfFines; // сумма пени // OSI-313
                        osvAbonent.ServicesSaldo[a.ServiceName].Kredit = a.Kredit * -1; // кредит
                        // 12-01-2023, shuma, по замечаниям в слаке
                        // https://grafltd.slack.com/archives/G01L2C71PP1/p1673434956300259
                        osvAbonent.ServicesSaldo[a.ServiceName].End += a.DebetWithoutFixes + a.SumOfFixes + a.Kredit // конечное
                            - (!forDebtors || DateTime.Today.Day >= 26 ? 0m : a.CurrentMonthAccurals); // OSI-261 текущие начисления попадают в конечное сальдо в отчете "Должники" только с 26 числа текущего месяца
                    }
                });
        }

        private static async Task<OSV> GetOSVOnPeriodByAbonents(DateTime onDate1, DateTime onDate2, IEnumerable<Abonent> abonents,
            bool forDebtors)
        {
            var response = CreateOSV(abonents);
            var abonentIds = abonents.Select(a => a.Id).ToArray();
            await FillOSVOnPeriod(onDate1, onDate2, response, t => abonentIds.Contains(t.AbonentId), forDebtors);
            return response;
        }

        private static async Task<OSV> GetOSVOnPeriodByOsi(DateTime onDate1, DateTime onDate2, Osi osi, bool forDebtors)
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonents = await db.Abonents.Where(a => a.OsiId == osi.Id).ToListAsync();
            var response = CreateOSV(abonents);
            await FillOSVOnPeriod(onDate1, onDate2, response, t => t.OsiId == osi.Id, forDebtors);

            // OSI-340 пункт 3: поправить выборку сальдо, чтобы не активные помещения туда не попадали. Тут есть нюанс, в отчетах за прошлые периоды, когда по этим помещениям были начисления, они должны отражаться и в отчетах и в сальдо.
            // если были неактивные абоненты в выборке и по ним нет конечного сальдо за период - значит абонент выбывает из выборки
            var filterAbonents = response.Abonents.Where(a => a.IsActive || a.ServicesSaldo.Sum(a => a.Value.SumOfAccurals) != 0).ToList();
            if (filterAbonents.Any())
            {
                response.Abonents = filterAbonents;
            }
            return response;
        }

        public static async Task<OSVAbonent> GetEndSaldoOnPeriodByAbonent(DateTime onDate1, DateTime onDate2, Abonent abonent,
            bool forDebtors = false)
        {
            var osv = await GetOSVOnDateByAbonents(onDate1, onDate2, new[] { abonent }, forDebtors);
            var osvAbonent = osv.Abonents[0];
            return osvAbonent;
        }

        public static async Task<OSV> GetOSVOnDateByAbonents(DateTime onDate1, DateTime onDate2, IEnumerable<Abonent> abonents,
            bool forDebtors = false)
        {
            var osv = await GetOSVOnPeriodByAbonents(onDate1, onDate2, abonents, forDebtors);
            return osv;
        }

        public static async Task<OSV> GetOSVOnDateByOsi(DateTime onDate1, DateTime onDate2, Osi osi, bool forDebtors = false)
        {
            var osv = await GetOSVOnPeriodByOsi(onDate1, onDate2, osi, forDebtors);
            return osv;
        }
    }
}
