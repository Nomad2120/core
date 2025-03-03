using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace OSI.Core.Services
{
    public interface IRegistrationAccountSvc
    {
        //errors
        string GetExceptionMessage(Exception ex);

        //check
        Task CheckRegistrationAccountById(int id);

        //get
        Task<RegistrationAccount> GetRegistrationAccountById(int id);
        Task<IEnumerable<RegistrationAccount>> GetRegistrationAccountsByRegistrationId(int registrationId);

        //crud
        Task<RegistrationAccount> AddOrUpdateRegistrationAccount(int id, RegistrationAccountRequest request);
        Task DeleteRegistrationAccount(int id);
    }

    public class RegistrationAccountSvc : IRegistrationAccountSvc
    {
        #region Конструктор
        private readonly IRegistrationSvc registrationSvc;
        private readonly ICatalogSvc catalogSvc;
        private readonly IAccountReportSvc accountReportSvc;
        private readonly IModelService<OSIBillingDbContext, AccountReportList> accountReportListSvc;

        public RegistrationAccountSvc(IRegistrationSvc registrationSvc,
                                      ICatalogSvc catalogSvc,
                                      IAccountReportSvc accountReportSvc,
                                      IModelService<OSIBillingDbContext, AccountReportList> accountReportListSvc)
        {
            this.registrationSvc = registrationSvc;
            this.catalogSvc = catalogSvc;
            this.accountReportSvc = accountReportSvc;
            this.accountReportListSvc = accountReportListSvc;
        }
        #endregion

        public string GetExceptionMessage(Exception ex)
        {
            string message = ex.Message;
            if (ex is DbUpdateException)
            {
                if (ex.InnerException.Message.IndexOf("duplicate key") > -1)
                {
                    if (ex.InnerException.Message.IndexOf("un_reg_accs") > -1)
                        message = "Такой счет уже есть";
                }
            }
            return message;
        }

        public async Task CheckRegistrationAccountById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (!await db.RegistrationAccounts.AnyAsync(o => o.Id == id))
                throw new Exception("Счет не найден");
        }

        public async Task<IEnumerable<RegistrationAccount>> GetRegistrationAccountsByRegistrationId(int registrationId)
        {
            await registrationSvc.CheckRegistrationById(registrationId);

            using var db = OSIBillingDbContext.DbContext;
            var registrationAccounts = await db.RegistrationAccounts
                .Include(s => s.AccountType)
                .Include(s => s.Bank)
                .Where(s => s.RegistrationId == registrationId)
                .ToListAsync();

            return registrationAccounts;
        }

        public async Task<RegistrationAccount> GetRegistrationAccountById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            
            var registrationAccount = await db.RegistrationAccounts
                .Include(s => s.AccountType)
                .Include(s => s.Bank)
                .FirstOrDefaultAsync(s => s.Id == id) ?? throw new Exception("Счет не найден");

            return registrationAccount;
        }

        private async Task CheckCanAdd(RegistrationAccountRequest request)
        {
            if (request.ServiceGroupId != null)
            {
                using var db = OSIBillingDbContext.DbContext;

                if (!await db.RegistrationAccounts
                    .AnyAsync(oa => oa.RegistrationId == request.RegistrationId && oa.AccountTypeCode == request.AccountTypeCode
                                 && oa.ServiceGroupId == null))
                    throw new Exception("Нет счета по умолчанию для данного типа");

                var serviceGroup = await db.ServiceGroups.FirstOrDefaultAsync(sg => sg.Id == request.ServiceGroupId);
                if (serviceGroup.AccountTypeCode != request.AccountTypeCode)
                    throw new Exception("Тип счета не совпадает с типом счета группы услуг");
            }
        }

        private void CheckCanUpdate(RegistrationAccount registrationAccount, RegistrationAccountRequest request)
        {
            if (request.RegistrationId != registrationAccount.RegistrationId)
                throw new Exception("Нельзя менять заявку");

            if (request.ServiceGroupId != registrationAccount.ServiceGroupId)
                throw new Exception("Нельзя менять группу услуг");

            if (request.AccountTypeCode != registrationAccount.AccountTypeCode && registrationAccount.ServiceGroupId != null)
                throw new Exception("Нельзя менять тип счета привязанного к группе услуг");
        }

        private async Task CheckCanDelete(RegistrationAccount registrationAccount)
        {
            if (registrationAccount.ServiceGroupId == null)
            {
                using var db = OSIBillingDbContext.DbContext;
                if (await db.RegistrationAccounts
                    .AnyAsync(oa => oa.RegistrationId == registrationAccount.RegistrationId && oa.AccountTypeCode == registrationAccount.AccountTypeCode
                                 && oa.ServiceGroupId != null))
                    throw new Exception("Нельзя удалить счет по умолчанию для данного типа, так как есть счета привязанные к группам услуг");
            }
        }

        public async Task<RegistrationAccount> AddOrUpdateRegistrationAccount(int id, RegistrationAccountRequest request)
        {
            await registrationSvc.CheckRegistrationById(request.RegistrationId);
            await catalogSvc.CheckBankByBic(request.BankBic);
            RegistrationAccount model = null;
            if (id == default)
            {
                model = new RegistrationAccount();
                await CheckCanAdd(request);
            }
            else
            {
                model = await GetRegistrationAccountById(id);
                CheckCanUpdate(model, request);
            }

            model.AccountType = null;
            model.Bank = null;
            model.Registration = null;
            model.ServiceGroup = null;
            model.Account = request.Account;
            model.AccountTypeCode = request.AccountTypeCode;
            model.BankBic = request.BankBic;
            model.RegistrationId = request.RegistrationId;
            model.ServiceGroupId = request.ServiceGroupId;

            using var db = OSIBillingDbContext.DbContext;
            if (id == default) db.RegistrationAccounts.Add(model);
            else db.RegistrationAccounts.Update(model);
            await db.SaveChangesAsync();

            // обновим модель для подгрузки связок
            if (id == default)
            {
                model = await GetRegistrationAccountById(model.Id);
                return model;
            }
            else return null;
        }

        public async Task DeleteRegistrationAccount(int id)
        {
            RegistrationAccount registrationAccount = await GetRegistrationAccountById(id);
            await CheckCanDelete(registrationAccount);

            using var db = OSIBillingDbContext.DbContext;
            db.RegistrationAccounts.Remove(registrationAccount);
            await db.SaveChangesAsync();
        }
    }
}
