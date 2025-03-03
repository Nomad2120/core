using ESoft.CommonLibrary;
using Microsoft.EntityFrameworkCore;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using OSI.Core.Models.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IPaymentOrderSvc
    {
        Task<ApiResponse<IEnumerable<Models.Banks.NotProcessedPayment>>> GetNotProcessedPayments(string bankCode, DateTime date);
        Task<ApiResponse> ProcessPayments(string bankCode, DateTime date);

        Task<ApiResponse<IEnumerable<Models.Banks.PaymentOrder>>> GetPaymentOrders(string bankCode, DateTime date);

        Task<ApiResponse<IEnumerable<SvodPaymentOrder>>> GetSvodPaymentOrdersByOsiId(int osiId, DateTime sd, DateTime ed);
    }

    public class PaymentOrderSvc : IPaymentOrderSvc
    {
        public const string OurName = "TOO «eOSI.kz»";
        public const string OurIdn = "230340040383";
        public const string OurAccount = "KZ05722S000029415875";
        public const string OurBic = "CASPKZKA";
        public const string OurKnp = "851";
        public const string OurKbe = "17";
        public const string OurAssign = "Комиссия eOsi.kz";

        private static readonly SemaphoreSlim semaphore = new(1, 1);

        public PaymentOrderSvc()
        {
        }

        private async Task<int> GetContractIdByCode(string bankCode)
        {
            using var db = OSIBillingDbContext.DbContext;
            int id = (await db.Contracts.FirstOrDefaultAsync(c => c.BankCode == bankCode.ToUpper()))?.Id ?? 0;
            return id;
        }

        private async Task<Contract> GetContractByCode(string bankCode)
        {
            using var db = OSIBillingDbContext.DbContext;
            return await db.Contracts.FirstOrDefaultAsync(c => c.BankCode == bankCode.ToUpper());
        }

        public async Task<ApiResponse<IEnumerable<Models.Banks.NotProcessedPayment>>> GetNotProcessedPayments(string bankCode, DateTime date)
        {
            var result = new ApiResponse<IEnumerable<Models.Banks.NotProcessedPayment>>();

            if (date >= DateTime.Today)
            {
                result.Code = 502;
                result.Message = "Допустимы только предыдущие дни";
                return result;
            }

            using var db = OSIBillingDbContext.DbContext;
            int contractId = await GetContractIdByCode(bankCode);
            if (contractId == 0)
            {
                result.Code = 301;
                result.Message = "Неверный код банка";
                return result;
            }

            var notProcessedPayments = await (from p in db.Payments
                                                  //join c in db.Contracts on p.ContractId equals c.Id
                                              join t in db.Transactions on p.Id equals t.PaymentId.Value
                                              //join a in db.Abonents on t.AbonentId equals a.Id
                                              where p.ContractId == contractId
                                                  && p.RegistrationDate.Date == date.Date
                                                  && p.State == "CREATED"
                                              //&& p.OsiId == 31 // test
                                              select new Models.Banks.NotProcessedPayment
                                              {
                                                  PaymentId = p.Id,
                                                  AbonentNum = p.AbonentNum ?? t.AbonentId.ToString(),
                                                  Amount = p.Amount,
                                                  PaymentDate = p.RegistrationDate,
                                                  Reference = p.Reference
                                              })
                                              .Distinct()
                                              .ToListAsync();

            if (notProcessedPayments?.Any() ?? false)
            {
                result.Result = notProcessedPayments;
            }
            else
            {
                result.Code = 501;
                result.Message = "Нет данных за указанную дату";
            }
            return result;
        }

        public async Task<ApiResponse> ProcessPayments(string bankCode, DateTime date)
        {
            var result = new ApiResponse();

            if (date >= DateTime.Today)
            {
                result.Code = 502;
                result.Message = "Допустимы только предыдущие дни";
                return result;
            }

            var contract = await GetContractByCode(bankCode);
            if (contract == null)
            {
                result.Code = 301;
                result.Message = "Неверный код банка";
                return result;
            }

            await semaphore.WaitAsync();
            try
            {
                using var db = OSIBillingDbContext.DbContext;
                using var dbTransaction = await db.Database.BeginTransactionAsync();

                try
                {
                    var preparedData = await (from p in db.Payments
                                              join o in db.Osies on p.OsiId equals o.Id
                                              join c in db.Contracts on p.ContractId equals c.Id
                                              join t in db.Transactions on p.Id equals t.PaymentId.Value
                                              join sg in db.ServiceGroups on t.GroupId equals sg.Id
                                              join oa in db.OsiAccounts on new
                                              {
                                                  OsiId = o.Id,
                                                  AccountTypeCode = sg.AccountTypeCode,
                                                  ServiceGroupId = (int?)sg.Id,
                                              } equals new
                                              {
                                                  OsiId = oa.OsiId,
                                                  AccountTypeCode = oa.AccountTypeCode,
                                                  ServiceGroupId = oa.ServiceGroupId,
                                              }
                                              into oaLeftJoin
                                              from oa in oaLeftJoin.DefaultIfEmpty()
                                              join oaDefault in db.OsiAccounts on new
                                              {
                                                  OsiId = o.Id,
                                                  AccountTypeCode = sg.AccountTypeCode,
                                                  ServiceGroupId = (int?)null,
                                              } equals new
                                              {
                                                  OsiId = oaDefault.OsiId,
                                                  AccountTypeCode = oaDefault.AccountTypeCode,
                                                  ServiceGroupId = oaDefault.ServiceGroupId,
                                              }
                                              where c.Id == contract.Id
                                                && p.RegistrationDate.Date == date.Date
                                                && p.State == "CREATED"
                                              //&& p.OsiId == 31 // test
                                              select new
                                              {
                                                  PaymentId = p.Id,
                                                  TransactionId = t.Id,
                                                  Name = o.Name,
                                                  Idn = o.Idn,
                                                  Account = oa.Account ?? oaDefault.Account,
                                                  Bic = oa.BankBic ?? oaDefault.BankBic,
                                                  Kbe = o.Kbe,
                                                  ContractId = c.Id,
                                                  OsiId = p.OsiId,
                                                  DtReg = date.Date,
                                                  GroupId = sg.Id,
                                                  Assign = sg.NameRu,
                                                  Amount = -t.Amount
                                              }).ToListAsync();
                    if (preparedData.Count == 0)
                    {
                        return result;
                    }
                    Dictionary<string, PaymentOrder> paymentOrders = new Dictionary<string, PaymentOrder>();
                    var paymentGroups = preparedData.GroupBy(x => x.PaymentId);
                    var payments = await db.Payments
                        .Include(p => p.Transactions)
                        .Where(p => p.Contract.Id == contract.Id
                            && p.RegistrationDate.Date == date.Date
                            && p.State == "CREATED")
                        .ToListAsync();
                    var paymentsTotalAmount = payments.Sum(p => p.Amount);
                    var totalAmountComission = contract.ComissionCalcType switch
                    {
                        ComissionCalcTypes.TotalAmountToEven => Math.Round(paymentsTotalAmount * contract.Comission / 100, 2),
                        ComissionCalcTypes.TotalAmountAwayFromZero => Math.Round(paymentsTotalAmount * contract.Comission / 100, 2, MidpointRounding.AwayFromZero),
                        ComissionCalcTypes.EachPaymentToEven or ComissionCalcTypes.EachPaymentAwayFromZero => payments.Sum(p => p.Comission),
                        _ => throw new InvalidOperationException("Неизвестный тип вычисления комиссии"),
                    };
                    var totalComissionSum = 0m;
                    PaymentOrder lastPaymentOrder = null;
                    foreach (var paymentGroup in paymentGroups)
                    {
                        Payment payment = payments.FirstOrDefault(p => p.Id == paymentGroup.Key)
                            ?? throw new Exception($"Платеж {paymentGroup.Key} не найден");
                        var paymentComission = contract.ComissionCalcType is ComissionCalcTypes.EachPaymentToEven or ComissionCalcTypes.EachPaymentAwayFromZero
                            ? payment.Comission
                            : Math.Round(payment.Amount / paymentsTotalAmount * totalAmountComission, 2);
                        totalComissionSum += paymentComission;
                        var comissionSum = 0m;
                        PaymentOrder paymentOrder = null;
                        foreach (var pd in paymentGroup)
                        {
                            // ищем во временном массиве данное платежное поручение
                            // создаем новый, если не найдем
                            string idx = $"{bankCode}/{pd.OsiId}/{pd.GroupId}";
                            if (!paymentOrders.ContainsKey(idx))
                            {
                                paymentOrder = new PaymentOrder
                                {
                                    Name = pd.Name,
                                    Idn = pd.Idn,
                                    Account = pd.Account,
                                    Bic = pd.Bic,
                                    Knp = "856",
                                    Kbe = pd.Kbe,
                                    ContractId = pd.ContractId,
                                    OsiId = pd.OsiId,
                                    DtReg = pd.DtReg,
                                    Assign = pd.Assign + $" #DT_{pd.DtReg.ToString("yyyy-MM-dd")}  #GP_{pd.GroupId}", //OSI-411
                                    ServiceGroupId = pd.GroupId,
                                    State = "CREATED",
                                    Amount = 0,
                                    ComisBank = 0,
                                    ComisOur = 0,
                                    CountPayments = 0
                                };
                                paymentOrders.Add(idx, paymentOrder);
                                db.PaymentOrders.Add(paymentOrder);
                            }
                            else
                                paymentOrder = paymentOrders[idx];

                            // проставляем сумму и комиссию банка
                            paymentOrder.Amount += pd.Amount;
                            var comission = Math.Round(pd.Amount / payment.Amount * paymentComission, 2);
                            comissionSum += comission;
                            paymentOrder.ComisBank += comission;
                            paymentOrder.CountPayments++;

                            // проводим платежи
                            Transaction transaction = payment.Transactions.FirstOrDefault(t => t.Id == pd.TransactionId)
                                ?? throw new Exception($"Транзакция {pd.TransactionId} не найдена");
                            transaction.PaymentOrder = paymentOrder;
                            db.Entry(transaction).Navigation(nameof(transaction.PaymentOrder)).IsModified = true;
                        }
                        paymentOrder.ComisBank -= comissionSum - paymentComission;
                        lastPaymentOrder = paymentOrder;
                        payment.State = "PROV";
                        payment.ProvDate = DateTime.Now;
                        db.Entry(payment).Property(p => p.State).IsModified = true;
                        db.Entry(payment).Property(p => p.ProvDate).IsModified = true;
                    }
                    lastPaymentOrder.ComisBank -= totalComissionSum - totalAmountComission;
                    await db.SaveChangesAsync();

                    // выбираем группы с текущим счетом
                    var currentAccountGroupsIds = await db.ServiceGroups.Where(g => g.AccountTypeCode == AccountTypeCodes.CURRENT).Select(g => g.Id).ToArrayAsync();

                    var paymentOrdersGroups = paymentOrders.Values.GroupBy(po => po.OsiId);
                    var osiIds = paymentOrdersGroups.Select(pog => pog.Key).ToArray();
                    var acts = await db.Acts
                        .Where(a => osiIds.Contains(a.OsiId) && a.Debt > 0 /*&& a.StateCode == ActStateCodes.SIGNED*/)
                        .ToListAsync();

                    // считаем нашу комиссию и проводим платежки
                    foreach (var pog in paymentOrdersGroups)
                    {
                        // ищем подписанные акты с ненулевой задолженностью нашей комиссии по данному ОСИ
                        var notProvedActs = acts.Where(a => a.OsiId == pog.Key).ToList();
                        foreach (var po in pog)
                        {
                            // только по группам с текущим счетом можно списывать нашу комиссию
                            if (currentAccountGroupsIds.Any(c => c == (po.ServiceGroupId ?? 0)))
                            {
                                // берем первый из актов
                                var notProvedAct = notProvedActs.FirstOrDefault();
                                if (notProvedAct != null)
                                {
                                    // делаем операцию по акту, чисто для истории
                                    ActOperation actOperation = new ActOperation
                                    {
                                        Dt = DateTime.Now,
                                        Amount = notProvedAct.Debt > (po.Amount - po.ComisBank) ? (po.Amount - po.ComisBank) : notProvedAct.Debt,
                                        PaymentOrderId = po.Id,
                                        ActId = notProvedAct.Id
                                    };
                                    db.ActOperations.Add(actOperation);
                                    // уменьшаем сумму несписанной комиссии в акте и если она нулевая, то проводим акт
                                    notProvedAct.Debt -= actOperation.Amount;
                                    db.Entry(notProvedAct).Property(p => p.Debt).IsModified = true;
                                    if (notProvedAct.Debt <= 0)
                                    {
                                        // PROV пока незачем ставить
                                        //notProvedAct.StateCode = ActStateCodes.PROV;
                                        //notProvedAct.State = null;
                                        // удаляем акт из списка найденных
                                        notProvedActs.Remove(notProvedAct);
                                    }
                                    // прописываем данную сумму в платежке как нашу комиссию
                                    po.ComisOur = actOperation.Amount;
                                    db.Entry(po).Property(p => p.ComisOur).IsModified = true;
                                }
                            }
                            po.State = "PROV";
                            db.Entry(po).Property(p => p.State).IsModified = true;
                        }
                    }
                    await db.SaveChangesAsync();

                    //Удерживаем косяки
                    if (await db.Failures.SumAsync(f => f.Amount) > 0)
                    {
                        foreach (var po in paymentOrders.Values)
                        {
                            var failureDebt = await db.Failures.Where(f => f.OsiId == po.OsiId && f.ServiceGroupId == po.ServiceGroupId).SumAsync(f => f.Amount);
                            if (failureDebt > 0)
                            {
                                //Если сумма косяка больше, чем можем удержать, то удерживаем, что можем
                                po.ComisFail = failureDebt > (po.Amount - po.ComisOur - po.ComisBank) ? (po.Amount - po.ComisOur - po.ComisBank) : failureDebt;
                                //Создаем запись для истории и уменьшения суммы
                                db.Failures.Add(new Failure
                                {
                                    OsiId = po.OsiId,
                                    ServiceGroupId = po.ServiceGroupId ?? 0,
                                    Dt = DateTime.Now,
                                    PaymentOrderId = po.Id,
                                    Amount = -po.ComisFail
                                });
                            }
                        }
                        await db.SaveChangesAsync();
                    }

                    await dbTransaction.CommitAsync();
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            }
            finally
            {
                semaphore.Release();
            }


            return result;
        }

        public async Task<ApiResponse<IEnumerable<Models.Banks.PaymentOrder>>> GetPaymentOrders(string bankCode, DateTime date)
        {
            var result = new ApiResponse<IEnumerable<Models.Banks.PaymentOrder>>();

            if (date >= DateTime.Today)
            {
                result.Code = 502;
                result.Message = "Допустимы только предыдущие дни";
                return result;
            }

            using var db = OSIBillingDbContext.DbContext;
            int contractId = await GetContractIdByCode(bankCode);
            if (contractId == 0)
            {
                result.Code = 301;
                result.Message = "Неверный код банка";
                return result;
            }

            var paymentOrders = await db.PaymentOrders.Where(p => p.ContractId == contractId && p.DtReg.Date == date.Date && p.State == "PROV").ToListAsync();
            if (paymentOrders.Any())
            {
                decimal comisOur = 0;
                List<Models.Banks.PaymentOrder> list = new List<Models.Banks.PaymentOrder>();
                foreach (PaymentOrder p in paymentOrders)
                {
                    // убираем платежки, где нашей комиссией "съело" всю сумму к перечислению
                    if (p.Amount - p.ComisOur - p.ComisBank - p.ComisFail > 0 || (bankCode.StartsWith("ERC_") && p.ComisBank > 0))
                    {
                        list.Add(new Models.Banks.PaymentOrder
                        {
                            Name = p.Name,
                            OsiId = bankCode.StartsWith("ERC_") ? p.OsiId : default(int?),
                            IDN = p.Idn,
                            IBAN = p.Account,
                            BIC = p.Bic,
                            KNP = p.Knp,
                            KBE = p.Kbe,
                            Assign = p.Assign,
                            ServiceId = bankCode.StartsWith("ERC_") ? p.ServiceGroupId : default(int?),
                            Date = p.DtReg,
                            Amount = p.Amount - p.ComisOur - p.ComisBank - p.ComisFail,
                            BankCommission = bankCode.StartsWith("ERC_") ? p.ComisBank : default(decimal?),
                        });
                    }
                    comisOur += p.ComisOur;
                }

                if (comisOur > 0)
                {
                    list.Add(new Models.Banks.PaymentOrder
                    {
                        Name = OurName,
                        OsiId = bankCode.StartsWith("ERC_") ? 0 : default(int?),
                        IDN = OurIdn,
                        IBAN = OurAccount,
                        BIC = OurBic,
                        KNP = OurKnp,
                        KBE = OurKbe,
                        Assign = OurAssign,
                        ServiceId = bankCode.StartsWith("ERC_") ? 0 : default(int?),
                        Date = date,
                        Amount = comisOur,
                        BankCommission = bankCode.StartsWith("ERC_") ? 0m : default(decimal?),
                    });
                }

                result.Result = list;
            }
            else
            {
                result.Code = 501;
                result.Message = "Нет данных за указанную дату";
            }

            return result;
        }

        public async Task<ApiResponse<IEnumerable<SvodPaymentOrder>>> GetSvodPaymentOrdersByOsiId(int osiId, DateTime sd, DateTime ed)
        {
            using var db = OSIBillingDbContext.DbContext;

            // подсчет по платежам с IBAN
            async Task<List<SvodPaymentOrder>> GetByPaymentsWithIBAN(DateTime d1, DateTime d2)
            {
                var firstCurrentAccountFromPaymentOrders = db.PaymentOrders
                    .Where(po => po.OsiId == osiId && po.ServiceGroup.AccountTypeCode == AccountTypeCodes.CURRENT)
                    .OrderBy(po => po.Id)
                    .FirstOrDefault()
                    ?.Account;
                var firstSavingsAccountFromPaymentOrders = db.PaymentOrders
                    .Where(po => po.OsiId == osiId && po.ServiceGroup.AccountTypeCode == AccountTypeCodes.SAVINGS)
                    .OrderBy(po => po.Id)
                    .FirstOrDefault()
                    ?.Account;
                return await (from p in db.Payments
                              join o in db.Osies on p.OsiId equals o.Id
                              join c in db.Contracts on p.ContractId equals c.Id
                              join t in db.Transactions on p.Id equals t.PaymentId.Value
                              join sg in db.ServiceGroups on t.GroupId equals sg.Id
                              join oa in db.OsiAccounts on new
                              {
                                  OsiId = o.Id,
                                  AccountTypeCode = sg.AccountTypeCode,
                                  ServiceGroupId = (int?)sg.Id,
                              } equals new
                              {
                                  OsiId = oa.OsiId,
                                  AccountTypeCode = oa.AccountTypeCode,
                                  ServiceGroupId = oa.ServiceGroupId,
                              }
                              into oaLeftJoin
                              from oa in oaLeftJoin.DefaultIfEmpty()
                              join oaDefault in db.OsiAccounts on new
                              {
                                  OsiId = o.Id,
                                  AccountTypeCode = sg.AccountTypeCode,
                                  ServiceGroupId = (int?)null,
                              } equals new
                              {
                                  OsiId = oaDefault.OsiId,
                                  AccountTypeCode = oaDefault.AccountTypeCode,
                                  ServiceGroupId = oaDefault.ServiceGroupId,
                              }
                              where p.OsiId == osiId
                                && p.RegistrationDate >= d1.Date
                                && p.RegistrationDate < d2.Date.AddDays(1)
                                && p.BankCode != "OSI"
                              group p by new
                              {
                                  p.Contract.BankName,
                                  sg.AccountTypeCode,
                                  Account = oa.Account ?? oaDefault.Account,
                                  p.RegistrationDate.Date,
                              } into g
                              select new SvodPaymentOrder
                              {
                                  BankName = g.Key.BankName,
                                  Date = g.Key.Date,
                                  IBAN = (g.Key.AccountTypeCode == AccountTypeCodes.CURRENT 
                                    ? firstCurrentAccountFromPaymentOrders 
                                    : firstSavingsAccountFromPaymentOrders) 
                                    ?? g.Key.Account,
                                  Amount = g.Sum(t => t.Amount),
                                  ComisBank = g.Sum(t => t.Comission),
                                  ComisOur = 0,
                                  AmountToTransfer = g.Sum(t => t.Amount) - g.Sum(t => t.Comission)
                              }).ToListAsync();
            }


            // подсчет по платежам
            //async Task<List<SvodPaymentOrder>> GetByPayments(DateTime d1, DateTime d2) => await db.Payments
            //    .Include(p => p.Contract)
            //    .Where(p => p.OsiId == osiId && p.RegistrationDate >= d1.Date && p.RegistrationDate < d2.Date.AddDays(1))
            //    .GroupBy(p => new { p.Contract.BankName, p.RegistrationDate.Date })
            //    .Select(g => new SvodPaymentOrder
            //    {
            //        BankName = g.Key.BankName,
            //        Date = g.Key.Date,
            //        Amount = g.Sum(t => t.Amount),
            //        ComisBank = g.Sum(t => t.Comission),
            //        ComisOur = 0,
            //        AmountToTransfer = g.Sum(t => t.Amount) - g.Sum(t => t.Comission)
            //    })
            //    .ToListAsync();

            // подсчет по ордерам
            async Task<List<SvodPaymentOrder>> GetByOrders(DateTime d1, DateTime d2) => await db.PaymentOrders
                .Include(p => p.Contract)
                .Where(p => p.OsiId == osiId && p.DtReg >= d1.Date && p.DtReg < d2.Date.AddDays(1))
                .GroupBy(p => new { p.Contract.BankName, p.Account, p.DtReg.Date })
                .Select(g => new SvodPaymentOrder
                {
                    BankName = g.Key.BankName,
                    Date = g.Key.Date,
                    IBAN = g.Key.Account,
                    Amount = g.Sum(t => t.Amount),
                    ComisBank = g.Sum(t => t.ComisBank),
                    ComisOur = g.Sum(t => t.ComisOur),
                    AmountToTransfer = g.Sum(t => t.Amount) - g.Sum(t => t.ComisBank) - g.Sum(t => t.ComisOur) - g.Sum(t => t.ComisFail)
                })
                .ToListAsync();

            DateTime keyDate = new DateTime(2021, 10, 24);
            var result = new ApiResponse<IEnumerable<SvodPaymentOrder>>();

            List<SvodPaymentOrder> svodPaymentOrders = new List<SvodPaymentOrder>();

            // начало до ключевой даты
            if (sd < keyDate)
            {
                // окончание после
                if (ed >= keyDate)
                {
                    svodPaymentOrders = await GetByPaymentsWithIBAN(sd, keyDate.AddDays(-1));
                    var addOrders = await GetByOrders(keyDate, ed);
                    svodPaymentOrders.AddRange(addOrders);
                }
                else // окончание тоже до ключевой даты
                {
                    svodPaymentOrders = await GetByPaymentsWithIBAN(sd, ed);
                }
            }
            else
            {
                // всё после ключевой даты
                svodPaymentOrders = await GetByOrders(sd, ed);
            }

            result.Result = svodPaymentOrders;

            return result;
        }
    }
}
