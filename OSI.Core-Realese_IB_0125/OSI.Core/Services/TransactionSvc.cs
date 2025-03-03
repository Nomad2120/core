using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OSI.Core.Models.Enums;
using OSI.Core.Models;
using OSI.Core.Models.Responses;
using OSI.Core.Models.Requests;
using OSI.Core.Models.Reports;
using OSI.Core.Models.Reports.SaldoOnAllPeriod;
using OSI.Core.Logic;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OSI.Core.Pages;

namespace OSI.Core.Services
{
    public interface ITransactionSvc
    {
        // get
        Task<Transaction> GetTransactionById(int id);

        // crud
        Task UpdateTransaction(int id, Transaction model);
        Task DeleteTransactionById(int id);

        // сальдо
        Task<EndSaldoResponse> GetEndSaldoOnDateByAbonentId(DateTime onDate, int abonentId);
        Task<EndSaldoResponse> GetEndSaldoOnDateByAbonent(DateTime onDate, Abonent abonent);
        Task<EndSaldoResponse> GetActiveEndSaldoOnDateByAbonentId(DateTime onDate, int abonentId);
        Task<EndSaldoResponse> GetActiveEndSaldoOnDateByAbonent(DateTime onDate, Abonent abonent);
        Task<OSVAbonent> GetEndSaldoOnPeriodByAbonentId(DateTime onDate1, DateTime onDate2, int abonentId);
        Task<OSVAbonent> GetEndSaldoOnPeriodByAbonent(DateTime onDate1, DateTime onDate2, Abonent abonent);
        Task<IEnumerable<SaldoPeriod>> GetEndSaldoOnAllPeriodByAbonentId(int abonentId);
        Task<IEnumerable<SaldoPeriod>> GetEndSaldoOnAllPeriodByAbonent(Abonent abonent);

        // оборотно-сальдовая ведомость по всем абонентам ОСИ
        Task<Models.Reports.OSV> GetOSVOnDateByOsiId(DateTime onDate1, DateTime onDate2, int osiId, bool forDebtors = false);
        Task<Models.Reports.OSV> GetOSVOnDateByOsi(DateTime onDate1, DateTime onDate2, Osi osi, bool forDebtors = false);

        // оборотно-сальдовая ведомость по выбранным абонентам
        Task<Models.Reports.OSV> GetOSVOnDateByAbonentIds(DateTime onDate1, DateTime onDate2, IEnumerable<int> abonentIds, bool forDebtors = false);
        Task<Models.Reports.OSV> GetOSVOnDateByAbonents(DateTime onDate1, DateTime onDate2, IEnumerable<Abonent> abonents, bool forDebtors = false);

        // платежи и корректировки
        Task<int> CreatePaymentTransactionById(int abonentId, int groupId, decimal amount, DateTime date, int paymentId);
        Task<int> CreatePaymentTransaction(Abonent abonent, ServiceGroup group, decimal amount, DateTime date, int paymentId);

        Task<ApiResponse<CreatePaymentResponse>> CreatePayment(string bankCode, int? userId, CreatePaymentRequest request);

        Task<ApiResponse<int>> CreatePaymentInternal(int? userId, CreatePaymentInternalRequest request);

        Task<int> CreateFixTransactionById(int abonentId, int serviceId, decimal amount, int paymentId);
        Task<int> CreateFixTransaction(Abonent abonent, OsiService osiService, decimal amount, int paymentId);

        Task<ApiResponse<int>> CreateFix(int userId, CreateFixRequest request);

        // начисления для тестов
        Task<int> CreateAccuralById(int abonentId, int osiServiceId, decimal amount);
        Task<int> CreateAccural(Abonent abonent, OsiService osiService, decimal amount);

        // начальное сальдо
        Task<int> CreateBeginSaldoById(int abonentId, int osiServiceId, decimal amount);
        Task<int> CreateBeginSaldo(Abonent abonent, OsiService osiService, decimal amount);

        // планы начислений
        Task<List<Transaction>> GetListTransactionsToCreateAccuralsByPlanId(int planAccuralId);
        Task CreateAccuralsByPlanId(int planAccuralId, bool ignoreAccuralCompleted = false);

        // платежи
        Task<List<PaymentTransaction>> GetPaymentsOnDateByOsiId(DateTime onDate1, DateTime onDate2, int osiId);
        Task<List<PaymentTransaction>> GetPaymentsOnDateByOsi(DateTime onDate1, DateTime onDate2, Osi osi);
        // корректировки
        Task<List<FixTransaction>> GetFixesOnDateByOsiId(DateTime onDate1, DateTime onDate2, int osiId);
        Task<List<FixTransaction>> GetFixesOnDateByOsi(DateTime onDate1, DateTime onDate2, Osi osi);

        Task<IEnumerable<Transaction>> GetTransactionsByAbonentId(int abonentId, DateTime onDate1, DateTime onDate2);
        Task<List<Models.Reports.AccuralsByAbonentAndServices.Group>> AccuralsByAbonentAndServices(int abonentId, DateTime onDate1, DateTime onDate2);
        Task CreateFine(int osiId, int year, int month);
    }

    public class TransactionSvc : ITransactionSvc
    {
        private readonly IPlanAccuralSvc planAccuralSvc;
        private readonly IAbonentSvc abonentSvc;
        private readonly IOsiServiceSvc osiServiceSvc;
        private readonly IServiceProvider serviceProvider;
        private readonly IBaseRateSvc baseRateSvc;

        public TransactionSvc(
            IPlanAccuralSvc planAccuralSvc,
            IAbonentSvc abonentSvc,
            IOsiServiceSvc osiServiceSvc,
            IServiceProvider serviceProvider,
            IBaseRateSvc baseRateSvc)
        {
            this.planAccuralSvc = planAccuralSvc;
            this.abonentSvc = abonentSvc;
            this.osiServiceSvc = osiServiceSvc;
            this.serviceProvider = serviceProvider;
            this.baseRateSvc = baseRateSvc;
        }

        public async Task<Transaction> GetTransactionById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            Transaction transaction = await db.Transactions.FirstOrDefaultAsync(t => t.Id == id);
            if (transaction == null)
                throw new Exception("Транзакция не найдена");
            return transaction;
        }

        public async Task UpdateTransaction(int id, Transaction model)
        {
            Transaction transaction = await GetTransactionById(id);
            transaction.AbonentId = model.AbonentId;
            transaction.Amount = model.Amount;
            transaction.Dt = model.Dt;
            transaction.GroupId = model.GroupId;
            transaction.OsiId = model.OsiId;
            transaction.OsiServiceId = model.OsiServiceId;
            transaction.PlanAccuralId = model.PlanAccuralId;
            transaction.TransactionType = model.TransactionType;
            using var db = OSIBillingDbContext.DbContext;
            db.Transactions.Update(transaction);
            await db.SaveChangesAsync();
        }

        public async Task DeleteTransactionById(int id)
        {
            Transaction transaction = await GetTransactionById(id);
            using var db = OSIBillingDbContext.DbContext;
            db.Transactions.Remove(transaction);
            await db.SaveChangesAsync();
        }

        public async Task<EndSaldoResponse> GetEndSaldoOnDateByAbonentId(DateTime onDate, int abonentId)
        {
            Abonent abonent = await abonentSvc.GetAbonentById(abonentId);
            var response = await GetEndSaldoOnDateByAbonent(onDate, abonent);
            return response;
        }

        public async Task<EndSaldoResponse> GetEndSaldoOnDateByAbonent(DateTime onDate, Abonent abonent)
        {
            if (abonent == null)
                throw new Exception("Укажите абонента");

            var result = await OSVLogic.GetEndSaldoOnDateByAbonent(onDate, abonent);
            return result;
        }

        public async Task<EndSaldoResponse> GetActiveEndSaldoOnDateByAbonentId(DateTime onDate, int abonentId)
        {
            Abonent abonent = await abonentSvc.GetAbonentById(abonentId);
            var response = await GetActiveEndSaldoOnDateByAbonent(onDate, abonent);
            return response;
        }

        public async Task<EndSaldoResponse> GetActiveEndSaldoOnDateByAbonent(DateTime onDate, Abonent abonent)
        {
            if (abonent == null)
                throw new Exception("Укажите абонента");

            var result = await OSVLogic.GetActiveEndSaldoOnDateByAbonent(onDate, abonent);
            return result;
        }

        public async Task<OSVAbonent> GetEndSaldoOnPeriodByAbonentId(DateTime onDate1, DateTime onDate2, int abonentId)
        {
            Abonent abonent = await abonentSvc.GetAbonentById(abonentId);
            var response = await GetEndSaldoOnPeriodByAbonent(onDate1, onDate2, abonent);
            return response;
        }

        public async Task<OSVAbonent> GetEndSaldoOnPeriodByAbonent(DateTime onDate1, DateTime onDate2, Abonent abonent)
        {
            var result = await OSVLogic.GetEndSaldoOnPeriodByAbonent(onDate1, onDate2, abonent);
            return result;
        }

        public async Task<IEnumerable<SaldoPeriod>> GetEndSaldoOnAllPeriodByAbonentId(int abonentId)
        {
            Abonent abonent = await abonentSvc.GetAbonentById(abonentId);
            var response = await GetEndSaldoOnAllPeriodByAbonent(abonent);
            return response;
        }

        public async Task<IEnumerable<SaldoPeriod>> GetEndSaldoOnAllPeriodByAbonent(Abonent abonent)
        {
            var result = await OSVLogic.GetEndSaldoOnAllPeriodByAbonent(abonent);
            return result;
        }

        public async Task<Models.Reports.OSV> GetOSVOnDateByOsiId(DateTime onDate1, DateTime onDate2, int osiId, bool forDebtors = false)
        {
            using var db = OSIBillingDbContext.DbContext;
            Osi osi = await db.Osies.FirstOrDefaultAsync(a => a.Id == osiId);
            if (osi == null)
                throw new Exception("ОСИ не найден");

            var response = await GetOSVOnDateByOsi(onDate1, onDate2, osi, forDebtors);

            return response;
        }

        public async Task<Models.Reports.OSV> GetOSVOnDateByOsi(DateTime onDate1, DateTime onDate2, Osi osi, bool forDebtors = false)
        {
            var result = await OSVLogic.GetOSVOnDateByOsi(onDate1, onDate2, osi, forDebtors);
            return result;
        }

        public async Task<Models.Reports.OSV> GetOSVOnDateByAbonentIds(DateTime onDate1, DateTime onDate2, IEnumerable<int> abonentIds,
            bool forDebtors = false)
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonents = await db.Abonents.Where(a => abonentIds.Contains(a.Id)).ToListAsync();
            if (abonents?.Any() != true)
                throw new Exception("Абоненты не найдены");

            var response = await GetOSVOnDateByAbonents(onDate1, onDate2, abonents, forDebtors);

            return response;
        }

        public async Task<Models.Reports.OSV> GetOSVOnDateByAbonents(DateTime onDate1, DateTime onDate2, IEnumerable<Abonent> abonents,
            bool forDebtors = false)
        {
            var result = await OSVLogic.GetOSVOnDateByAbonents(onDate1, onDate2, abonents, forDebtors);
            return result;
        }

        public async Task<int> CreatePaymentTransactionById(int abonentId, int groupId, decimal amount, DateTime date, int paymentId)
        {
            using var db = OSIBillingDbContext.DbContext;
            Abonent abonent = await abonentSvc.GetAbonentById(abonentId);
            ServiceGroup group = await db.ServiceGroups.FirstOrDefaultAsync(s => s.Id == groupId);
            if (group == null)
                throw new Exception("Группа не найдена");
            int transactionId = await CreatePaymentTransaction(abonent, group, amount, date, paymentId);
            return transactionId;
        }

        public async Task<int> CreatePaymentTransaction(Abonent abonent, ServiceGroup group, decimal amount, DateTime date, int paymentId)
        {
            if (amount <= 0)
                throw new Exception("Сумма должна быть больше нуля");

            using var db = OSIBillingDbContext.DbContext;
            Transaction transaction = new Transaction
            {
                AbonentId = abonent.Id,
                Dt = date,
                Amount = -amount, // отрицательное
                OsiId = abonent.OsiId,
                GroupId = group.Id,
                TransactionType = TransactionTypeCodes.PAY,
                PaymentId = paymentId
            };
            db.Transactions.Add(transaction);
            await db.SaveChangesAsync();

            return transaction.Id;
        }

        public async Task<ApiResponse<CreatePaymentResponse>> CreatePayment(string bankCode, int? userId, CreatePaymentRequest request)
        {
            ApiResponse<CreatePaymentResponse> apiResponse = new();
            using var db = OSIBillingDbContext.DbContext;
            Contract contract = await db.Contracts.FirstOrDefaultAsync(c => c.BankCode == bankCode);
            Payment payment = await db.Payments.Include(p => p.Transactions).FirstOrDefaultAsync(p => p.Reference == request.Reference && p.ContractId == contract.Id);
            if (payment == null)
            {
                Abonent abonent = await abonentSvc.GetAbonentForPaymentService(request.AbonentNum);
                if (userId.HasValue && !db.Users.Any(u => u.Id == userId))
                {
                    throw new Exception("Пользователь не найден");
                }
                decimal amount = request.Services.Where(s => s.Sum > 0m).Sum(s => s.Sum);
                payment = new Payment
                {
                    ContractId = contract.Id,
                    BankCode = contract.BankCode,
                    RegistrationDate = contract.BankCode.StartsWith("ERC_") ? request.Date : DateTime.Now,
                    Reference = request.Reference,
                    State = "CREATED",
                    Amount = amount,
                    Comission = contract.ComissionCalcType switch
                    {
                        ComissionCalcTypes.EachPaymentToEven => Math.Round(amount * contract.Comission / 100, 2),
                        ComissionCalcTypes.EachPaymentAwayFromZero => Math.Round(amount * contract.Comission / 100, 2, MidpointRounding.AwayFromZero),
                        ComissionCalcTypes.TotalAmountToEven or ComissionCalcTypes.TotalAmountAwayFromZero => 0m,
                        _ => throw new InvalidOperationException("Неизвестный тип вычисления комиссии"),
                    },
                    OsiId = abonent.OsiId,
                    UserId = userId,
                    AbonentNum = request.AbonentNum
                };
                db.Payments.Add(payment);
                await db.SaveChangesAsync();
                List<int> createdTransactionIds = new();
                try
                {
                    foreach (var service in request.Services.Where(s => s.Sum > 0m))
                    {
                        createdTransactionIds.Add(await CreatePaymentTransactionById(abonent.Id, service.ServiceId, service.Sum, payment.RegistrationDate, payment.Id));
                    }
                }
                catch
                {
                    foreach (var id in createdTransactionIds)
                    {
                        await DeleteTransactionById(id);
                    }
                    db.Payments.Remove(payment);
                    await db.SaveChangesAsync();
                    throw;
                }
            }
            else
            {
                var isError = false;
                if (payment.Transactions.Count != request.Services.Where(s => s.Sum > 0m).Count())
                {
                    isError = true;
                }
                if (!isError)
                {
                    Abonent abonent = await abonentSvc.GetAbonentForPaymentService(request.AbonentNum);
                    if (payment.Transactions.Any(t =>
                        {
                            var service = request.Services.Where(s => s.Sum > 0m).FirstOrDefault(s => s.ServiceId == t.GroupId);
                            if (service == null) return true;
                            if (t.AbonentId != abonent.Id || t.OsiId != abonent.OsiId || t.TransactionType != TransactionTypeCodes.PAY || t.Amount != -service.Sum) return true;
                            return false;
                        }))
                    {
                        isError = true;
                    }
                }
                if (isError)
                {
                    apiResponse.Code = 201;
                    apiResponse.Message = "Такой референс уже зарегистрирован";
                }
            }
            apiResponse.Result = new()
            {
                PaymentId = payment.Id,
                RegistrationDate = DateTime.Parse(payment.RegistrationDate.ToString("s")),
                Reference = request.Reference,
            };
            return apiResponse;
        }

        public async Task<ApiResponse<int>> CreatePaymentInternal(int? userId, CreatePaymentInternalRequest request)
        {
            // проверки
            var today = DateTime.Today;
            var nextMonth = today.AddMonths(1);
            if (request.Date < new DateTime(today.Year, today.Month, 1) || request.Date.Date > today)
            {
                throw new Exception("Дата платежа должна быть в пределах первого и текущего числа этого месяца");
            }

            using var db = OSIBillingDbContext.DbContext;
            Contract contract = await db.Contracts.FirstOrDefaultAsync(c => c.BankCode == "OSI");
            if (contract == null)
            {
                throw new Exception("Контракт OSI не найден");
            }

            Abonent abonent = await abonentSvc.GetAbonentById(request.AbonentNum);

            if (userId.HasValue && !db.Users.Any(u => u.Id == userId))
            {
                throw new Exception("Пользователь не найден");
            }

            var serviceGroups = await db.ServiceGroups.ToListAsync();
            foreach (var service in request.Services)
            {
                if (!serviceGroups.Any(s => s.Id == service.ServiceGroupId))
                    throw new Exception($"Группа ID={service.ServiceGroupId} не найдена");
                if (service.Sum <= 0)
                    throw new Exception($"Услуга {service.ServiceGroupId} группы: Сумма должна быть больше нуля");
            }

            string reference;
            Payment payment;
            do
            {
                reference = Guid.NewGuid().ToString();
                payment = await db.Payments.FirstOrDefaultAsync(p => p.Reference == reference && p.ContractId == contract.Id);
            }
            while (payment != null);

            decimal amount = request.Services.Sum(s => s.Sum);

            // транзакция
            ApiResponse<int> apiResponse = new();
            using var dbTransaction = await db.Database.BeginTransactionAsync();
            try
            {
                payment = new Payment
                {
                    ContractId = contract.Id,
                    BankCode = contract.BankCode,
                    RegistrationDate = request.Date,
                    Reference = reference,
                    State = "CREATED",
                    Amount = amount,
#if !DEBUG
                    Comission = 0,
#else
                    Comission = Math.Round(amount * contract.Comission / 100, 2), // для тестирования с комиссией
#endif
                    OsiId = abonent.OsiId,
                    UserId = userId
                };
                foreach (var service in request.Services)
                {
                    payment.Transactions.Add(new Transaction
                    {
                        AbonentId = abonent.Id,
                        Dt = request.Date,
                        Amount = -service.Sum, // отрицательное
                        OsiId = abonent.OsiId,
                        GroupId = service.ServiceGroupId,
                        TransactionType = TransactionTypeCodes.PAY
                    });
                }

                db.Payments.Add(payment);
                await db.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
            apiResponse.Result = payment.Id;
            return apiResponse;
        }

        public async Task<int> CreateFixTransactionById(int abonentId, int serviceId, decimal amount, int fixId)
        {
            using var db = OSIBillingDbContext.DbContext;
            Abonent abonent = await abonentSvc.GetAbonentById(abonentId);
            OsiService osiService = await db.OsiServices.FirstOrDefaultAsync(os => os.Id == serviceId && os.OsiId == abonent.OsiId);
            if (osiService == null)
                throw new Exception("Услуга не найдена в ОСИ");
            int transactionId = await CreateFixTransaction(abonent, osiService, amount, fixId);
            return transactionId;
        }

        public async Task<int> CreateFixTransaction(Abonent abonent, OsiService osiService, decimal amount, int fixId)
        {
            using var db = OSIBillingDbContext.DbContext;
            Transaction transaction = new Transaction
            {
                AbonentId = abonent.Id,
                Dt = DateTime.Now,
                Amount = amount,
                OsiId = abonent.OsiId,
                OsiServiceId = osiService.Id,
                GroupId = osiService.ServiceGroupId,
                TransactionType = TransactionTypeCodes.FIX,
                FixId = fixId
            };
            db.Transactions.Add(transaction);
            await db.SaveChangesAsync();

            return transaction.Id;
        }

        public async Task<ApiResponse<int>> CreateFix(int userId, CreateFixRequest request)
        {
            ApiResponse<int> apiResponse = new();
            using var db = OSIBillingDbContext.DbContext;
            Abonent abonent = await abonentSvc.GetAbonentById(request.AbonentNum);
            if (!db.Users.Any(u => u.Id == userId))
            {
                throw new Exception("Пользователь не найден");
            }
            decimal amount = request.Services.Sum(s => s.Sum);
            var fix = new Fix
            {
                Reason = request.Reason,
                RegistrationDate = DateTime.Now,
                Amount = amount,
                OsiId = abonent.OsiId,
                UserId = userId
            };
            db.Fixes.Add(fix);
            await db.SaveChangesAsync();
            List<int> createdTransactionIds = new();
            try
            {
                foreach (var service in request.Services)
                {
                    createdTransactionIds.Add(await CreateFixTransactionById(request.AbonentNum, service.ServiceId, service.Sum, fix.Id));
                }
            }
            catch
            {
                foreach (var id in createdTransactionIds)
                {
                    await DeleteTransactionById(id);
                }
                db.Fixes.Remove(fix);
                await db.SaveChangesAsync();
                throw;
            }
            apiResponse.Result = fix.Id;
            return apiResponse;
        }

        public async Task<List<PaymentTransaction>> GetPaymentsOnDateByOsiId(DateTime onDate1, DateTime onDate2, int osiId)
        {
            using var db = OSIBillingDbContext.DbContext;
            Osi osi = await db.Osies.FirstOrDefaultAsync(a => a.Id == osiId);
            if (osi == null)
                throw new Exception("ОСИ не найден");

            var payments = await GetPaymentsOnDateByOsi(onDate1, onDate2, osi);

            return payments;
        }

        public async Task<List<PaymentTransaction>> GetPaymentsOnDateByOsi(DateTime onDate1, DateTime onDate2, Osi osi)
        {
            if (osi == null)
                throw new Exception("Укажите ОСИ");

            using var db = OSIBillingDbContext.DbContext;

            // платежи за период
            var payments = await db.Transactions
                .Include(t => t.Group)
                .Include(t => t.Abonent)
                .Include(t => t.Payment)
                .ThenInclude(p => p.Contract)
                .Where(t => t.Dt >= onDate1.Date && t.Dt < onDate2.Date.AddDays(1) && t.OsiId == osi.Id && t.TransactionType == TransactionTypeCodes.PAY)
                .Select(t => new PaymentTransaction
                {
                    Dt = t.Dt,
                    AbonentName = t.Abonent.Name,
                    Flat = t.Abonent.Flat,
                    ServiceName = t.Group.NameRu,
                    Amount = t.Amount * -1,
                    BankName = t.Payment.Contract.BankName
                })
                .ToListAsync();

            return payments;
        }

        public async Task<List<FixTransaction>> GetFixesOnDateByOsiId(DateTime onDate1, DateTime onDate2, int osiId)
        {
            using var db = OSIBillingDbContext.DbContext;
            Osi osi = await db.Osies.FirstOrDefaultAsync(a => a.Id == osiId);
            if (osi == null)
                throw new Exception("ОСИ не найден");

            var fixes = await GetFixesOnDateByOsi(onDate1, onDate2, osi);

            return fixes;
        }

        public async Task<List<FixTransaction>> GetFixesOnDateByOsi(DateTime onDate1, DateTime onDate2, Osi osi)
        {
            if (osi == null)
                throw new Exception("Укажите ОСИ");

            using var db = OSIBillingDbContext.DbContext;

            // платежи за период
            var fixes = await db.Transactions
                .Include(t => t.OsiService)
                .Include(t => t.Group)
                .Include(t => t.Abonent)
                .Include(t => t.Fix)
                .Where(t => t.Dt >= onDate1.Date && t.Dt < onDate2.Date.AddDays(1) && t.OsiId == osi.Id && t.TransactionType == TransactionTypeCodes.FIX)
                .Select(t => new FixTransaction
                {
                    Dt = t.Dt,
                    AbonentName = t.Abonent.Name,
                    Flat = t.Abonent.Flat,
                    ServiceName = t.OsiService.NameRu,
                    ServiceGroupName = t.Group.NameRu,
                    Reason = t.Fix.Reason,
                    Amount = t.Amount,
                })
                .ToListAsync();

            return fixes;
        }

        public async Task<int> CreateAccuralById(int abonentId, int osiServiceId, decimal amount)
        {
            using var db = OSIBillingDbContext.DbContext;
            Abonent abonent = await abonentSvc.GetAbonentById(abonentId);
            OsiService osiService = await osiServiceSvc.GetOsiServiceById(osiServiceId);
            int transactionId = await CreateAccural(abonent, osiService, amount);
            return transactionId;
        }

        public async Task<int> CreateAccural(Abonent abonent, OsiService osiService, decimal amount)
        {
            if (abonent == null)
                throw new Exception("Укажите абонента");

            if (osiService == null)
                throw new Exception("Укажите услугу");

            if (amount <= 0)
                throw new Exception("Сумма должна быть больше нуля");

            using var db = OSIBillingDbContext.DbContext;
            Transaction transaction = new Transaction
            {
                AbonentId = abonent.Id,
                Dt = DateTime.Now,
                Amount = amount,
                OsiId = abonent.OsiId,
                OsiServiceId = osiService.Id,
                GroupId = osiService.ServiceGroupId,
                TransactionType = TransactionTypeCodes.ACC
            };
            db.Transactions.Add(transaction);
            await db.SaveChangesAsync();

            return transaction.Id;
        }

        public async Task<int> CreateBeginSaldoById(int abonentId, int osiServiceId, decimal amount)
        {
            using var db = OSIBillingDbContext.DbContext;
            Abonent abonent = await abonentSvc.GetAbonentById(abonentId);
            OsiService osiService = await osiServiceSvc.GetOsiServiceById(osiServiceId);
            int transactionId = await CreateBeginSaldo(abonent, osiService, amount);
            return transactionId;
        }

        public async Task<int> CreateBeginSaldo(Abonent abonent, OsiService osiService, decimal amount)
        {
            if (amount == 0)
                throw new Exception("Сумма должна отличаться от нуля");

            using var db = OSIBillingDbContext.DbContext;
            Transaction transaction = new Transaction
            {
                AbonentId = abonent.Id,
                Dt = DateTime.Now,
                Amount = amount,
                OsiId = abonent.OsiId,
                OsiServiceId = osiService.Id,
                GroupId = osiService.ServiceGroupId,
                TransactionType = TransactionTypeCodes.SALDO
            };
            db.Transactions.Add(transaction);
            await db.SaveChangesAsync();

            return transaction.Id;
        }

        public async Task<List<Transaction>> GetListTransactionsToCreateAccuralsByPlanId(int planAccuralId)
        {
            var plan = await planAccuralSvc.GetPlanAccuralById(planAccuralId);
            return await GetListTransactionsToCreateAccuralsByPlan(plan);
        }

        private async Task<List<Transaction>> GetListTransactionsToCreateAccuralsByPlan(PlanAccural plan)
        {
            using var db = OSIBillingDbContext.DbContext;

            var abonents = await db.Abonents
                .Where(o => o.OsiId == plan.OsiId && o.IsActive).ToListAsync();

            // OSI-296 начисления с тиын, если флаг AccuralsWithDecimals установлен, то округляем до сотых
            int roundDecimals = !plan.Osi.AccuralsWithDecimals ? 0 : 2;

            List<Transaction> transactions = new List<Transaction>();

            // последнее число расчетного месяца
            DateTime calcDate = plan.BeginDate.AddMonths(1).AddDays(-1);

            var osiServices = await db.OsiServices.Include(a => a.ServiceGroup).Where(o => o.OsiId == plan.OsiId && o.IsActive & o.OsiServiceAmounts.Any()).ToListAsync();
            var osiConnectedServices = await db.ConnectedServices.Where(a => a.OsiId == plan.OsiId && a.Dt <= calcDate).ToListAsync();
            var osiServiceAmounts = await db.OsiServiceAmounts.Include(a => a.AccuralMethod).Where(a => a.OsiId == plan.OsiId).ToListAsync();
            var osiParkingPlaces = await db.ParkingPlaces.Where(a => a.OsiId == plan.OsiId).ToListAsync();

            try
            {
                foreach (var osiService in osiServices)
                {
                    var osiServiceAmount = osiServiceAmounts.OrderByDescending(o => o.Dt).FirstOrDefault(o => o.OsiServiceId == osiService.Id) ?? 
                        throw new Exception($"OsiServiceId={osiService.Id}: по данной услуге не задана сумма");

                    decimal abonentsTotalSquare = 0; // общая приведенная площадь помещений                
                    decimal abonentsTotalEffectiveSquare = 0; // общая приведенная полезная площадь помещений

                    var abonentConnectings = new List<ConnectedService>();

                    // проверяем участвующих один раз и записываем в массив
                    var connectedServices = osiConnectedServices.Where(o => o.OsiServiceId == osiService.Id).OrderByDescending(o => o.Dt).ToList();
                    foreach (var abonent in abonents)
                    {
                        var abonentConnecting = connectedServices.FirstOrDefault(a => a.AbonentId == abonent.Id);
                        if (abonentConnecting?.IsActive ?? false)
                        {
                            abonentConnecting.Abonent = abonent;
                            abonentConnectings.Add(abonentConnecting);
                            // площадь только участвующих абонентов подсчитаем заранее
                            abonentsTotalSquare += abonent.Square;
                            abonentsTotalEffectiveSquare += (abonent.EffectiveSquare > 0 ? abonent.EffectiveSquare.Value : abonent.Square);
                        }
                    }

                    var parkingPlaces = new List<ParkingPlace>();
                    if (osiService.ServiceGroup.Code == "PARKING")
                    {
                        parkingPlaces = osiParkingPlaces.Where(o => o.OsiServiceId == osiService.Id).OrderByDescending(o => o.Dt).ToList();
                    }

                    foreach (var abonentConnecting in abonentConnectings)
                    {
                        // считаем сумму начисления
                        decimal accuralAmount = 0;
                        switch (osiServiceAmount.AccuralMethod.Code)
                        {
                            case "TARIF_1KVM":
                                accuralAmount = osiServiceAmount.Amount * abonentConnecting.Abonent.Square; //* (abonent.AreaTypeCode != AreaTypeCodes.RESIDENTIAL ? plan.Osi.CoefUnlivingArea * 1.0m / 100 : 1);
                                break;
                            case "TARIF_1KVM_EFF":
                                accuralAmount = osiServiceAmount.Amount * (abonentConnecting.Abonent.EffectiveSquare > 0 ? abonentConnecting.Abonent.EffectiveSquare.Value : abonentConnecting.Abonent.Square); //* (abonent.AreaTypeCode != AreaTypeCodes.RESIDENTIAL ? plan.Osi.CoefUnlivingArea * 1.0m / 100 : 1);
                                break;
                            case "FIX_SUM_FLAT":
                                if (osiService.ServiceGroup.Code == "PARKING")
                                {
                                    var parkingPlace = parkingPlaces.FirstOrDefault(o => o.AbonentId == abonentConnecting.Abonent.Id);
                                    accuralAmount = osiServiceAmount.Amount * (parkingPlace?.Places ?? 0);
                                }
                                else
                                    accuralAmount = osiServiceAmount.Amount;
                                break;
                            case "OB_SUM_KVM":
                                // OSI-117, OSI-118
                                // котельная, считается так: общая сумма / общую квадратуру всех помещений * квадратуру помещения абонента
                                // Например общая сумма на все помещения 10000 тг. Общая площадь всех помещений (50 + 40 + 60 + 70) = 220 кв.м.
                                // Получается, помещение с 50 кв.м. должно оплатить 10000 / 220 * 50 = 2272,72 тг, помещение с 40 кв.м.: 10000 / 220 * 40 = 1818,18 тг
                                accuralAmount = osiServiceAmount.Amount / abonentsTotalSquare * abonentConnecting.Abonent.Square;
                                break;
                            case "OB_SUM_KVM_EFF":
                                accuralAmount = osiServiceAmount.Amount / abonentsTotalEffectiveSquare * (abonentConnecting.Abonent.EffectiveSquare > 0 ? abonentConnecting.Abonent.EffectiveSquare.Value : abonentConnecting.Abonent.Square);
                                break;
                            case "OB_SUM_FLAT":
                                accuralAmount = osiServiceAmount.Amount / abonentConnectings.Count;
                                break;
                            default:
                                break;
                        }

                        if (accuralAmount > 0)
                        {
                            // создаем проводку
                            Transaction transaction = new Transaction
                            {
                                AbonentId = abonentConnecting.Abonent.Id,
                                Amount = Math.Round(accuralAmount, roundDecimals, MidpointRounding.AwayFromZero), // OSI-296 начисления с тиын
                                OsiId = plan.OsiId,
                                OsiServiceId = osiService.Id,
                                GroupId = osiService.ServiceGroupId,
                                PlanAccuralId = plan.Id,
                                Dt = DateTime.Now,
                                TransactionType = TransactionTypeCodes.ACC,
                                OsiServiceAmountId = osiServiceAmount.Id // OSI-230, 19-02-2023, shuma
                            };
                            transactions.Add(transaction);
                        }
                    }

                    // Нужно по этому взносу ставить услугу не активной, после начисления, и абонентов на услуге не отключать. Обсудили вчера.
                    // https://grafltd.slack.com/archives/G01L2C71PP1/p1668847956998299?thread_ts=1668763645.086119&cid=G01L2C71PP1
                    if (abonentConnectings.Any() && !osiService.ServiceGroup.CopyToNextPeriod)
                    {
                        OsiService os = JsonSerializer.Deserialize<OsiService>(JsonSerializer.Serialize(osiService));
                        os.IsActive = false;
                        db.Update(os);
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw;  // для установки брекпоинта
            }

            return transactions;
        }

        public async Task CreateAccuralsByPlanId(int planAccuralId, bool ignoreAccuralCompleted = false)
        {
            using var db = OSIBillingDbContext.DbContext;

            var plan = await planAccuralSvc.GetPlanAccuralById(planAccuralId);
            if (plan.AccuralCompleted && !ignoreAccuralCompleted)
                throw new Exception("Начисления по данному плану уже производились");

            // удаляем старые начисления, если были
            bool accuralsExist = await db.Transactions.AnyAsync(a => a.PlanAccuralId == plan.Id);
            if (accuralsExist)
            {
                // на всякий уточним тип транзакции
                var accurals = await db.Transactions.Where(a => a.PlanAccuralId == plan.Id && a.TransactionType == TransactionTypeCodes.ACC).ToListAsync();
                db.RemoveRange(accurals);
                await db.SaveChangesAsync();
            }

            // тариф для ОСИ вычисляем в этот момент
            plan.Tariff = await OsiTariffLogic.GetOsiTariffValueByDate(plan.OsiId, DateTime.Today);

            var transactions = await GetListTransactionsToCreateAccuralsByPlan(plan);

            // помечаем план как успешно завершенный
            if (transactions.Any())
            {
                db.Transactions.AddRange(transactions);
                // сохраняем кол-во помещений на момент начислений, чтобы потом считать сумму для акта
                // заодно обновляем и на самом ОСИ
                int count = await db.Abonents.CountAsync(a => a.OsiId == plan.Osi.Id && !a.External && a.IsActive);
                plan.Osi.ApartCount = count;
                plan.AccuralCompleted = true;
                plan.AccuralDate = DateTime.Today;
                plan.ApartCount = count;
                db.PlanAccurals.Update(plan);
                await db.SaveChangesAsync();
                _ = serviceProvider.GetRequiredService<ITelegramSubscriptionBotSvc>().SendPdfForOsiSubscriptions(plan.OsiId);
            }
            else
                throw new Exception("Не создано ни одного начисления");
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByAbonentId(int abonentId, DateTime onDate1, DateTime onDate2)
        {
            using var db = OSIBillingDbContext.DbContext;
            await abonentSvc.CheckAbonentById(abonentId);
            var transactions = await db.Transactions
                .Include(t => t.Group)
                .Include(t => t.OsiService).ThenInclude(s => s.ServiceGroup)
                .Include(t => t.PlanAccural)
                .Include(t => t.Payment)
                .Where(t => t.AbonentId == abonentId && t.Dt.Date >= onDate1.Date && t.Dt.Date <= onDate2.Date)
                .ToListAsync();
            return transactions;
        }

        public async Task<List<Models.Reports.AccuralsByAbonentAndServices.Group>> AccuralsByAbonentAndServices(int abonentId, DateTime onDate1, DateTime onDate2)
        {
            using var db = OSIBillingDbContext.DbContext;
            await abonentSvc.CheckAbonentById(abonentId);

            var data = db.Transactions
                .Include(t => t.Group)
                .Include(t => t.OsiService)
                .Where(t =>
                t.AbonentId == abonentId &&
                (t.TransactionType == TransactionTypeCodes.ACC || t.TransactionType == TransactionTypeCodes.FIX) &&
                t.Dt >= onDate1.Date && t.Dt < onDate2.Date.AddDays(1))
                .OrderBy(t => t.GroupId)
                .ThenBy(t => t.OsiServiceId)
                .GroupBy(t => new { GroupName = t.Group.NameRu, ServiceName = t.OsiService.NameRu })
                .Select(g => new
                {
                    g.Key.GroupName,
                    g.Key.ServiceName,
                    Accural = g.Where(s => s.TransactionType == TransactionTypeCodes.ACC).Sum(s => s.Amount),
                    Fix = g.Where(s => s.TransactionType == TransactionTypeCodes.FIX).Sum(s => s.Amount),
                    Total = g.Sum(s => s.Amount)
                })
                .ToList();

            List<Models.Reports.AccuralsByAbonentAndServices.Group> report = new();
            foreach (var group in data.GroupBy(t => t.GroupName))
            {
                var repRecord = new Models.Reports.AccuralsByAbonentAndServices.Group
                {
                    GroupName = group.First().GroupName,
                    Services = new List<Models.Reports.AccuralsByAbonentAndServices.Service>()
                };
                foreach (var service in group)
                {
                    repRecord.Services.Add(new Models.Reports.AccuralsByAbonentAndServices.Service
                    {
                        ServiceName = service.ServiceName,
                        Accural = service.Accural,
                        Fix = service.Fix,
                        Total = service.Total
                    });
                }
                report.Add(repRecord);
            }

            return report;
        }

        public async Task CreateFine(int osiId, int year, int month)
        {
            await FineLogic.CreateFine(osiId, year, month, await baseRateSvc.GetBaseRate(year, month));
        }
    }
}
