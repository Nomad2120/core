using Microsoft.EntityFrameworkCore;
using OSI.Core.Comparer;
using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using OSI.Core.Models.Responses;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IPastDebtSvc
    {
        Task<PastDebtsResponse> GetPastDebts(int abonentId, int serviceGroupId);

        Task<IEnumerable<PastDebtsByOsiResponse>> GetPastDebtsByOsiId(int osiId);

        Task SavePastDebts(int abonentId, int serviceGroupId, IEnumerable<PastDebtInfo> pastDebts);

        //OSI-131
        Task<DebtorNotificationResponse> GetDebtorNotification(int abonentId);

        //OSI-132
        Task<NotaryApplicationResponse> GetNotaryApplicationWithRegistry(int abonentId);
    }

    public class PastDebtSvc : ModelService<OSIBillingDbContext, PastDebt>, IPastDebtSvc
    {
        private readonly IAbonentSvc abonentSvc;
        private readonly IOsiSvc osiSvc;
        private readonly IServiceGroupSaldoSvc serviceGroupSaldoSvc;
        private readonly ITransactionSvc transactionSvc;

        public PastDebtSvc(IAbonentSvc abonentSvc, IOsiSvc osiSvc, IServiceGroupSaldoSvc serviceGroupSaldoSvc, ITransactionSvc transactionSvc)
        {
            this.abonentSvc = abonentSvc;
            this.osiSvc = osiSvc;
            this.serviceGroupSaldoSvc = serviceGroupSaldoSvc;
            this.transactionSvc = transactionSvc;
        }

        public async Task<PastDebtsResponse> GetPastDebts(int abonentId, int serviceGroupId)
        {
            var db = DbContext;
            Abonent abonent = await abonentSvc.GetAbonentById(abonentId);
            ServiceGroup serviceGroup = await db.ServiceGroups.FirstOrDefaultAsync(s => s.Id == serviceGroupId);
            if (serviceGroup == null)
                throw new Exception("Группа услуг не найдена");
            var firstOsiTransactionDt = await db
                .Transactions
                .Where(t => t.OsiId == abonent.OsiId && (t.TransactionType == TransactionTypeCodes.ACC || t.TransactionType == TransactionTypeCodes.PAY))
                .OrderBy(t => t.Dt)
                .Select(t => t.Dt)
                .FirstOrDefaultAsync();
            var maxPeriod =
                firstOsiTransactionDt == default ?
                throw new Exception("Невозможно определить месяц включения ОСИ") :
                new DateTime(firstOsiTransactionDt.Date.Year, firstOsiTransactionDt.Date.Month, 1).AddMonths(-1);
            var pastDebts = await db
                .PastDebts
                .Where(pd => pd.AbonentId == abonentId && pd.ServiceGroupId == serviceGroupId && pd.Period.Year >= 2019 && pd.Period <= maxPeriod)
                .Select(pd => new PastDebtInfo { Period = pd.Period, Amount = pd.Amount })
                .ToListAsync();
            var period = maxPeriod;
            var saldo = (await db
                .ServiceGroupSaldos
                .FirstOrDefaultAsync(sgs => sgs.AbonentId == abonentId && sgs.GroupId == serviceGroupId))
                ?.Saldo ?? 0m;
            while (period.Year >= 2019)
            {
                PastDebtInfo pastDebt = pastDebts.FirstOrDefault(pd => pd.Period == period);
                if (pastDebt == null)
                {
                    pastDebts.Add(new PastDebtInfo { Period = period, Amount = period != maxPeriod ? 0 : saldo });
                }
                else if (period == maxPeriod)
                {
                    pastDebt.Amount = saldo;
                }
                period = period.AddMonths(-1);
            }
            return new PastDebtsResponse { PastDebts = pastDebts.OrderByDescending(pd => pd.Period), Saldo = saldo };
        }

        public async Task<IEnumerable<PastDebtsByOsiResponse>> GetPastDebtsByOsiId(int osiId)
        {
            var db = DbContext;
            await osiSvc.CheckOsiById(osiId);
            var firstOsiTransactionDt = await db
                .Transactions
                .Where(t => t.OsiId == osiId && (t.TransactionType == TransactionTypeCodes.ACC || t.TransactionType == TransactionTypeCodes.PAY))
                .OrderBy(t => t.Dt)
                .Select(t => t.Dt)
                .FirstOrDefaultAsync();
            var maxPeriod =
                firstOsiTransactionDt == default ?
                throw new Exception("Невозможно определить месяц включения ОСИ") :
                new DateTime(firstOsiTransactionDt.Date.Year, firstOsiTransactionDt.Date.Month, 1).AddMonths(-1);
            var pastDebts = await db
                .PastDebts
                .Include(pd => pd.Abonent)
                .Include(pd => pd.ServiceGroup)
                .Where(pd => pd.Abonent.OsiId == osiId && pd.Period.Year >= 2019 && pd.Period <= maxPeriod && pd.Amount != 0)
                .ToListAsync();
            var saldos = await db
                .ServiceGroupSaldos
                .Include(sgs => sgs.Abonent)
                .Include(sgs => sgs.Group)
                .Where(sgs => sgs.OsiId == osiId && sgs.Saldo != 0)
                .ToListAsync();

            foreach (var saldo in saldos)
            {
                var pastDebt = pastDebts.FirstOrDefault(pd => pd.AbonentId == saldo.AbonentId && pd.ServiceGroupId == saldo.GroupId && pd.Period == maxPeriod);
                if (pastDebt != null)
                {
                    pastDebt.Amount = saldo.Saldo;
                }
                else
                {
                    pastDebts.Add(new PastDebt
                    {
                        AbonentId = saldo.AbonentId,
                        Abonent = saldo.Abonent,
                        ServiceGroupId = saldo.GroupId,
                        ServiceGroup = saldo.Group,
                        Period = maxPeriod,
                        Amount = saldo.Saldo,
                    });
                }
            }
            pastDebts.RemoveAll(pd => pd.Amount == 0);
            var result = new List<PastDebtsByOsiResponse>();
            foreach (var abonentGroup in pastDebts.GroupBy(pd => pd.AbonentId))
            {
                var serviceGroups = new List<PastDebtsByOsiResponseItem>();
                foreach (var group in abonentGroup.GroupBy(pd => pd.ServiceGroupId))
                {
                    var serviceGroup = group.First().ServiceGroup;
                    serviceGroups.Add(new()
                    {
                        ServiceGroupId = group.Key,
                        ServiceGroupNameRu = serviceGroup.NameRu,
                        ServiceGroupNameKz = serviceGroup.NameKz,
                        PastDebts = group.OrderByDescending(pd => pd.Period).Select(pd => new PastDebtInfo { Period = pd.Period, Amount = pd.Amount }),
                    });
                }
                var abonent = abonentGroup.First().Abonent;
                var flat = abonent.Flat;
                if (abonent.AreaTypeCode == AreaTypeCodes.NON_RESIDENTIAL || abonent.AreaTypeCode == AreaTypeCodes.BASEMENT)
                {
                    if (await db.Abonents.AnyAsync(z => z.OsiId == abonent.OsiId && z.Flat == abonent.Flat && z.AreaTypeCode == AreaTypeCodes.RESIDENTIAL))
                    {
                        flat += abonent.AreaTypeCode switch
                        {
                            AreaTypeCodes.NON_RESIDENTIAL => "Н",
                            AreaTypeCodes.BASEMENT => "П",
                            _ => ""
                        };
                    }
                }
                result.Add(new()
                {
                    AbonentId = abonentGroup.Key,
                    AbonentName = abonent.Name,
                    Flat = flat,
                    ServiceGroups = serviceGroups,
                });
            }
            return result.OrderBy(a => a.Flat, NaturalComparer.Instance).ToList(); // OSI-268, 25-04-2023, shuma
        }

        public async Task SavePastDebts(int abonentId, int serviceGroupId, IEnumerable<PastDebtInfo> pastDebts)
        {
            var db = DbContext;
            Abonent abonent = await abonentSvc.GetAbonentById(abonentId);
            ServiceGroup serviceGroup = await db.ServiceGroups.FirstOrDefaultAsync(s => s.Id == serviceGroupId);
            if (serviceGroup == null)
                throw new Exception("Группа услуг не найдена");
            var firstOsiTransactionDt = await db
                .Transactions
                .Where(t => t.OsiId == abonent.OsiId && (t.TransactionType == TransactionTypeCodes.ACC || t.TransactionType == TransactionTypeCodes.PAY))
                .OrderBy(t => t.Dt)
                .Select(t => t.Dt)
                .FirstOrDefaultAsync();
            var maxPeriod =
                firstOsiTransactionDt == default ?
                throw new Exception("Невозможно определить месяц включения ОСИ") :
                new DateTime(firstOsiTransactionDt.Date.Year, firstOsiTransactionDt.Date.Month, 1).AddMonths(-1);
            var dbPastDebts = await db
                .PastDebts
                .Where(pd => pd.AbonentId == abonentId && pd.ServiceGroupId == serviceGroupId)
                .ToListAsync();
            pastDebts = pastDebts.Where(pd => pd.Period.Year >= 2019 && new DateTime(pd.Period.Year, pd.Period.Month, 1) <= maxPeriod);
            if (!pastDebts.Any())
                return;
            foreach (var pastDebt in pastDebts)
            {
                pastDebt.Period = new DateTime(pastDebt.Period.Year, pastDebt.Period.Month, 1);
                var dbPastDebt = dbPastDebts.FirstOrDefault(pd => pd.Period == pastDebt.Period);
                if (dbPastDebt == null)
                {
                    dbPastDebt = new PastDebt
                    {
                        AbonentId = abonentId,
                        ServiceGroupId = serviceGroupId,
                        Period = pastDebt.Period,
                        Amount = pastDebt.Amount,
                    };
                    db.PastDebts.Add(dbPastDebt);
                    dbPastDebts.Add(dbPastDebt);
                }
                else
                {
                    dbPastDebt.Amount = pastDebt.Amount;
                    db.PastDebts.Update(dbPastDebt);
                }
            }
            await db.SaveChangesAsync();
            var pastDebtMaxPeriod = dbPastDebts.FirstOrDefault(pd => pd.Period == maxPeriod);
            if (pastDebtMaxPeriod == null)
                return;
            var serviceGroupSaldo = await db
                .ServiceGroupSaldos
                .FirstOrDefaultAsync(sgs => sgs.AbonentId == abonentId && sgs.GroupId == serviceGroupId);
            if ((serviceGroupSaldo?.Saldo ?? 0m) != pastDebtMaxPeriod.Amount)
            {
                if (serviceGroupSaldo == null)
                {
                    await serviceGroupSaldoSvc.AddServiceGroupSaldoByRequest(new Models.Requests.ServiceGroupSaldoRequest
                    {
                        AbonentId = abonent.Id,
                        GroupId = serviceGroupId,
                        OsiId = abonent.OsiId,
                        Saldo = pastDebtMaxPeriod.Amount,
                    });
                }
                else
                {
                    await serviceGroupSaldoSvc.UpdateServiceGroupSaldoAmountById(serviceGroupSaldo.Id, pastDebtMaxPeriod.Amount);
                }
            }
        }

        //OSI-131
        public async Task<DebtorNotificationResponse> GetDebtorNotification(int abonentId)
        {
            var abonent = await abonentSvc.GetAbonentById(abonentId);
            var osi = await osiSvc.GetOsiById(abonent.OsiId);
            DateTime dt = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1);
            var osv = await transactionSvc.GetEndSaldoOnPeriodByAbonent(dt, dt, abonent);

            var debtorNotificationResponse = new DebtorNotificationResponse
            {
                DebtDate = dt,
                Flat = abonent.Flat,
                OsiId = osi.Id,
                OsiName = osi.Name,
                OsiChairman = osi.Fio,
                Address = osi.Address + ", кв. " + abonent.Flat,
                ServicesDebts = osv.ServicesSaldo.Select(x => new DebtorNotificationResponseItem
                {
                    Saldo = x.Value.End,
                    ServiceName = x.Key,
                    ServiceNameKz = x.Value.ServiceNameKz
                })
            };

            return debtorNotificationResponse;
        }

        //OSI-132
        public async Task<NotaryApplicationResponse> GetNotaryApplicationWithRegistry(int abonentId)
        {
            //ToTitleCase для перевода первой буквы в верхний регистр, т.к. в разных языках и/или фреймворках(.Net Framework, .Net Core) названия месяцев то с большой то с маленькой буквы начинаются
            static string GetPeriod(DateTime dt)
            {
                CultureInfo cultureInfo = CultureInfo.GetCultureInfo("ru-RU");
                return cultureInfo.TextInfo.ToTitleCase(dt.ToString("MMMM yyyy", cultureInfo.DateTimeFormat));
            }

            DateTime GetDateTime(string period) => DateTime.ParseExact(period, "MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat);

            var db = DbContext;
            var abonent = await db
                .Abonents
                .Include(a => a.AreaType)
                .Include(a => a.Osi)
                .Include(a => a.PastDebts)
                .ThenInclude(pd => pd.ServiceGroup)
                .FirstOrDefaultAsync(a => a.Id == abonentId);
            if (abonent == null)
                throw new Exception("Абонент не найден");
            DateTime debtDate = DateTime.Today.AddDays(-1);
            NotaryApplicationResponse notaryApplicationResponse = new()
            {
                OsiId = abonent.Osi.Id,
                OsiName = abonent.Osi.Name,
                OsiIdn = abonent.Osi.Idn,
                OsiAddress = abonent.Osi.Address,
                OsiChairman = abonent.Osi.Fio,
                OsiPhone = abonent.Osi.Phone,
                AbonentName = abonent.Name,
                AbonentIdn = abonent.Idn,
                AbonentFlat = abonent.Flat,
                AbonentAddress = abonent.Osi.Address + ", кв. " + abonent.Flat,
                AbonentPhone = abonent.Phone,
                DebtDate = DateTime.SpecifyKind(debtDate, DateTimeKind.Unspecified),
            };

            var firstOsiTransactionDt = await db
                .Transactions
                .Where(t => t.OsiId == abonent.OsiId && (t.TransactionType == TransactionTypeCodes.ACC || t.TransactionType == TransactionTypeCodes.PAY))
                .OrderBy(t => t.Dt)
                .Select(t => t.Dt)
                .FirstOrDefaultAsync();
            var maxPeriod =
                firstOsiTransactionDt == default ?
                throw new Exception("Невозможно определить месяц включения ОСИ") :
                new DateTime(firstOsiTransactionDt.Date.Year, firstOsiTransactionDt.Date.Month, 1).AddMonths(-1);

            var servicesWithZeroEnd = new List<string>();
            var serviceGroups = await db.ServiceGroups.ToListAsync();
            var services = serviceGroups.Select(x => x.NameRu).ToList();

            bool firstSaldoPeriod = true;
            var saldoPeriods = await transactionSvc.GetEndSaldoOnAllPeriodByAbonent(abonent);
            foreach (var saldoPeriod in saldoPeriods.OrderByDescending(x => x.Period))
            {
                servicesWithZeroEnd.AddRange(services.Except(saldoPeriod.Services.Select(s => s.ServiceName)).Except(servicesWithZeroEnd));
                foreach (var service in saldoPeriod.Services)
                {
                    string serviceName = service.ServiceName;
                    if (servicesWithZeroEnd.Contains(serviceName))
                    {
                        continue;
                    }
                    if (service.End <= 0)
                    {
                        servicesWithZeroEnd.Add(serviceName);
                        continue;
                    }
                    var group = notaryApplicationResponse.Registry.FirstOrDefault(g => g.ServiceName == serviceName);
                    if (group == null)
                    {
                        group = new NotaryApplicationRegistryGroup
                        {
                            ServiceName = serviceName,
                            ServiceNameKz = service.ServiceNameKz,
                        };
                        notaryApplicationResponse.Registry.Add(group);
                    }
                    var debt = service.DebetWithoutFixes + service.SumOfFixes - service.Kredit - service.SumOfUnpaidFines;
                    if (service.Begin < 0)
                    {
                        debt += service.Begin;
                    }
                    if (firstSaldoPeriod && DateTime.Today.Day < 26)
                    {
                        debt -= service.SumOfAccurals;
                    }
                    group.Debts.ForEach(d => d.CumulativeDebt += debt);
                    group.Debts.Add(new NotaryApplicationRegistryItem
                    {
                        Period = saldoPeriod.PeriodDescription,
                        Debt = debt,
                        CumulativeDebt = debt,
                    });
                }
                firstSaldoPeriod = false;
            }
            var period = maxPeriod;
            while (period.Year >= 2019)
            {
                var pastDebts = period != maxPeriod
                                ? abonent.PastDebts.Where(pd => pd.Period == period)
                                : await db.ServiceGroupSaldos
                                .Include(sgs => sgs.Group)
                                .Where(sgs => sgs.AbonentId == abonentId)
                                .Select(sgs => new PastDebt
                                {
                                    Period = period,
                                    Amount = sgs.Saldo,
                                    ServiceGroupId = sgs.GroupId,
                                    ServiceGroup = sgs.Group,
                                    AbonentId = abonentId
                                })
                                .ToListAsync();
                if (!pastDebts.Any())
                {
                    break;
                }
                servicesWithZeroEnd.AddRange(services.Except(pastDebts.Select(pd => pd.ServiceGroup.NameRu)).Except(servicesWithZeroEnd));
                foreach (var pastDebt in pastDebts)
                {
                    string serviceName = pastDebt.ServiceGroup.NameRu;
                    if (servicesWithZeroEnd.Contains(serviceName))
                    {
                        continue;
                    }
                    if (pastDebt.Amount <= 0)
                    {
                        servicesWithZeroEnd.Add(serviceName);
                        continue;
                    }
                    var group = notaryApplicationResponse.Registry.FirstOrDefault(g => g.ServiceName == serviceName);
                    if (group == null)
                    {
                        group = new NotaryApplicationRegistryGroup
                        {
                            ServiceName = serviceName,
                            ServiceNameKz = pastDebt.ServiceGroup.NameKz,
                        };
                        notaryApplicationResponse.Registry.Add(group);
                    }
                    var debt = pastDebt.Amount - (abonent.PastDebts.FirstOrDefault(pd => pd.Period == pastDebt.Period.AddMonths(-1) && pd.ServiceGroupId == pastDebt.ServiceGroupId)?.Amount ?? 0m);
                    group.Debts.ForEach(d => d.CumulativeDebt += debt);
                    group.Debts.Add(new NotaryApplicationRegistryItem
                    {
                        Period = GetPeriod(period),
                        Debt = debt,
                        CumulativeDebt = debt,
                    });
                }
                period = period.AddMonths(-1);
            }

            notaryApplicationResponse.Registry.ForEach(g =>
            {
                g.Debts.Reverse();
                int number = 1;
                g.Debts.ForEach(d => d.Number = number++);
            });

            return notaryApplicationResponse;
        }
    }
}
