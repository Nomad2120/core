using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IOsiAccountSvc
    {
        //errors
        string GetExceptionMessage(Exception ex);

        //check
        Task CheckOsiAccountById(int id);

        //get
        Task<OsiAccount> GetOsiAccountById(int id);
        Task<IEnumerable<OsiAccount>> GetOsiAccountsByOsiId(int osiId);

        //crud
        Task<OsiAccount> AddOrUpdateOsiAccount(int id, OsiAccountRequest request);
        Task DeleteOsiAccount(int id);
        Task CheckCanAdd(OsiAccountRequest request);
        Task CheckCanUpdate(OsiAccount osiAccount, OsiAccountRequest request);
        Task CheckCanDelete(OsiAccount osiAccount);
    }

    public class OsiAccountSvc : IOsiAccountSvc
    {
        #region Конструктор
        private readonly IOsiSvc osiSvc;
        private readonly ICatalogSvc catalogSvc;
        private readonly IAccountReportSvc accountReportSvc;
        private readonly IModelService<OSIBillingDbContext, AccountReportList> accountReportListSvc;

        public OsiAccountSvc(IOsiSvc osiSvc, ICatalogSvc catalogSvc,
            IAccountReportSvc accountReportSvc, IModelService<OSIBillingDbContext, AccountReportList> accountReportListSvc)
        {
            this.osiSvc = osiSvc;
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
                    if (ex.InnerException.Message.IndexOf("unq_osi_accounts") > -1)
                        message = "Такой счет уже есть на данном ОСИ";
                }
            }
            return message;
        }

        public async Task CheckOsiAccountById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (!await db.OsiAccounts.AnyAsync(o => o.Id == id))
                throw new Exception("Счет ОСИ не найден");
        }

        public async Task<IEnumerable<OsiAccount>> GetOsiAccountsByOsiId(int osiId)
        {
            await osiSvc.CheckOsiById(osiId);
            using var db = OSIBillingDbContext.DbContext;
            var osiAccounts = await db.OsiAccounts
                .Include(s => s.AccountType)
                .Include(s => s.Bank)
                .Where(s => s.OsiId == osiId).ToListAsync();
            return osiAccounts;
        }

        public async Task<OsiAccount> GetOsiAccountById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var osiAccount = await db.OsiAccounts
                .Include(s => s.AccountType)
                .Include(s => s.Bank)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (osiAccount == null)
                throw new Exception("Счет ОСИ не найден");

            return osiAccount;
        }

        public async Task CheckCanAdd(OsiAccountRequest request)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (request.ServiceGroupId != null)
            {
                if (!await db.OsiAccounts
                    .AnyAsync(oa => oa.OsiId == request.OsiId && oa.AccountTypeCode == request.AccountTypeCode
                                 && oa.ServiceGroupId == null))
                    throw new Exception("Нет счета по умолчанию для данного типа");

                var serviceGroup = await db.ServiceGroups.FirstOrDefaultAsync(sg => sg.Id == request.ServiceGroupId);
                if (serviceGroup?.AccountTypeCode != request.AccountTypeCode)
                    throw new Exception("Тип счета не совпадает с типом счета группы услуг");
            }
            else
            {
                if (await db.OsiAccounts
                    .AnyAsync(oa => oa.OsiId == request.OsiId && oa.AccountTypeCode == request.AccountTypeCode
                                 && oa.ServiceGroupId == null))
                    throw new Exception("Уже есть счет для данного типа");
            }
        }

        public async Task CheckCanUpdate(OsiAccount osiAccount, OsiAccountRequest request)
        {
            if (request.OsiId != osiAccount.OsiId)
                throw new Exception("Нельзя менять ОСИ");
            if (request.ServiceGroupId != osiAccount.ServiceGroupId)
                throw new Exception("Нельзя менять группу услуг");

            using var db = OSIBillingDbContext.DbContext;

            if (osiAccount.ServiceGroupId == null)
            {
                if (request.AccountTypeCode != osiAccount.AccountTypeCode)
                {
                    bool servicesExist = await db.OsiServices
                        .Include(s => s.ServiceGroup)
                        .AnyAsync(o => o.OsiId == osiAccount.OsiId && o.ServiceGroup.AccountTypeCode == osiAccount.AccountTypeCode);
                    if (servicesExist)
                        throw new Exception("По данному счету уже имеются услуги, менять тип нельзя");
                }
            }
            else
            {
                if (request.AccountTypeCode != osiAccount.AccountTypeCode)
                    throw new Exception("Нельзя менять тип счета привязанного к группе услуг");
            }
        }

        public async Task CheckCanDelete(OsiAccount osiAccount)
        {
            using var db = OSIBillingDbContext.DbContext;

            if (osiAccount.ServiceGroupId == null)
            {
                bool servicesExist = await db.OsiServices
                    .Include(s => s.ServiceGroup)
                    .AnyAsync(o => o.OsiId == osiAccount.OsiId && o.ServiceGroup.AccountTypeCode == osiAccount.AccountTypeCode);
                if (servicesExist)
                    throw new Exception("По данному счету уже имеются услуги, удалять нельзя");

                if (await db.OsiAccounts
                    .AnyAsync(oa => oa.OsiId == osiAccount.OsiId && oa.AccountTypeCode == osiAccount.AccountTypeCode
                                 && oa.ServiceGroupId != null))
                    throw new Exception("Нельзя удалить счет по умолчанию для данного типа, так как есть счета привязанные к группам услуг");
            }
        }

        public async Task<OsiAccount> AddOrUpdateOsiAccount(int id, OsiAccountRequest request)
        {
            await osiSvc.CheckOsiById(request.OsiId);
            await catalogSvc.CheckBankByBic(request.BankBic);
            OsiAccount model = null;
            if (id == default)
            {
                model = new OsiAccount();
                await CheckCanAdd(request);
            }
            else
            {
                model = await GetOsiAccountById(id);
                await CheckCanUpdate(model, request);
            }


            model.AccountType = null;
            model.Bank = null;
            model.Osi = null;
            model.ServiceGroup = null;
            model.Account = request.Account;
            model.AccountTypeCode = request.AccountTypeCode;
            model.BankBic = request.BankBic;
            model.OsiId = request.OsiId;
            model.ServiceGroupId = request.ServiceGroupId;

            using var db = OSIBillingDbContext.DbContext;
            if (id == default) db.OsiAccounts.Add(model);
            else db.OsiAccounts.Update(model);
            await db.SaveChangesAsync();

            await accountReportSvc.AddList(model, DateTime.Today.AddDays(1 - DateTime.Today.Day));

            // обновим модель для подгрузки связок
            if (id == default)
            {
                model = await GetOsiAccountById(model.Id);
                return model;
            }
            else return null;
        }

        public async Task DeleteOsiAccount(int id)
        {
            OsiAccount osiAccount = await GetOsiAccountById(id);
            await CheckCanDelete(osiAccount);

            using var db = OSIBillingDbContext.DbContext;
            db.OsiAccounts.Remove(osiAccount);
            await db.SaveChangesAsync();
        }
    }
}
