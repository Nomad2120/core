using ESoft.CommonLibrary;
using Microsoft.EntityFrameworkCore;
using OSI.Core.Helpers;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace OSI.Core.Services
{
    public interface IOsiAccountApplicationSvc
    {
        Task<IEnumerable<OsiAccountApplication>> GetOsiAccountApplications();
        Task<IEnumerable<OsiAccountApplication>> GetOsiAccountApplicationsByOsiId(int osiId);
        Task<IEnumerable<OsiAccountApplicationDoc>> GetOsiAccountApplicationDocs(int id);
        Task CheckActiveApplication(OsiAccountApplicationCheckRequest request);
        Task<OsiAccountApplication> CreateOsiAccountApplication(OsiAccountApplicationRequest request);
        Task<OsiAccountApplication> GetOsiAccountApplicationById(int id);
        Task<OsiAccountApplicationDoc> AddDoc(int id, AddScanDoc request);
        Task DeleteDoc(int id, int docId);
        Task Approve(int id);
        Task Reject(int id, string reason);
    }

    public class OsiAccountApplicationSvc : IOsiAccountApplicationSvc
    {
        private readonly IOsiSvc osiSvc;
        private readonly ICatalogSvc catalogSvc;
        private readonly IOsiAccountSvc osiAccountSvc;
        private readonly IScanSvc scanSvc;
        private readonly ITelegramBotSvc telegramBotSvc;

        public OsiAccountApplicationSvc(IOsiSvc osiSvc, ICatalogSvc catalogSvc,
            IOsiAccountSvc osiAccountSvc, IScanSvc scanSvc, ITelegramBotSvc telegramBotSvc)
        {
            this.osiSvc = osiSvc;
            this.catalogSvc = catalogSvc;
            this.osiAccountSvc = osiAccountSvc;
            this.scanSvc = scanSvc;
            this.telegramBotSvc = telegramBotSvc;
        }

        public async Task<IEnumerable<OsiAccountApplication>> GetOsiAccountApplications()
        {
            using var db = OSIBillingDbContext.DbContext;
            return await db.OsiAccountApplications
                .Include(x => x.Osi)
                .Include(x => x.OsiAccount)
                .Include(x => x.Bank)
                .Include(x => x.AccountType)
                .ToListAsync();
        }

        public async Task<IEnumerable<OsiAccountApplication>> GetOsiAccountApplicationsByOsiId(int osiId)
        {
            using var db = OSIBillingDbContext.DbContext;
            return await db.OsiAccountApplications
                .Include(x => x.Bank)
                .Include(x => x.AccountType)
                .Where(x => x.OsiId == osiId).ToListAsync();
        }

        public async Task<OsiAccountApplication> GetOsiAccountApplicationById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var osiAccountApplication = await db.OsiAccountApplications
                .Include(x => x.Osi)
                .Include(x => x.OsiAccount)
                .Include(x => x.Bank)
                .Include(x => x.AccountType)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Заявка не найдена");
            return osiAccountApplication;
        }

        public async Task<IEnumerable<OsiAccountApplicationDoc>> GetOsiAccountApplicationDocs(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            return await db.OsiAccountApplicationDocs
                .Include(x => x.Scan)
                .Include(x => x.DocType)
                .Where(x => x.OsiAccountApplicationId == id)
                .ToListAsync();
        }

        public async Task CheckActiveApplication(OsiAccountApplicationCheckRequest request)
        {
            using var db = OSIBillingDbContext.DbContext;
            var activeApplication = await db.OsiAccountApplications
                .If(request.ApplicationType == "UPDATE")
                .Where(x => x.OsiAccountId == request.OsiAccountId)
                .EndIf()
                .FirstOrDefaultAsync(x => x.OsiId == request.OsiId
                                          && x.ApplicationType == request.ApplicationType
                                          && x.AccountTypeCode == request.AccountTypeCode
                                          && x.ServiceGroupId == request.ServiceGroupId
                                          && x.State.In("CREATED", "PENDING"));
            if (activeApplication != null)
            {
                await telegramBotSvc.SendOsiAccountApplicationNotification(activeApplication, repeat: true);
                throw new Exception("Существует активная заявка " +
                    request.ApplicationType switch
                    {
                        "ADD" => "на добавление счета",
                        "UPDATE" => "на изменение счета",
                        //"REMOVE" => "на удаление счета",
                        _ => "на изменение счета"
                    });
            }
        }

        public async Task<OsiAccountApplication> CreateOsiAccountApplication(OsiAccountApplicationRequest request)
        {
            await CheckActiveApplication(new()
            {
                OsiId = request.OsiId,
                OsiAccountId = request.OsiAccountId,
                ApplicationType = request.ApplicationType,
                AccountTypeCode = request.AccountTypeCode,
                ServiceGroupId = request.ServiceGroupId,
            });

            await osiSvc.CheckOsiById(request.OsiId);
            await catalogSvc.CheckBankByBic(request.BankBic);

            OsiAccount osiAccount = null;

            if (request.ApplicationType == "ADD")
            {
                request.OsiAccountId = null;
                if (request.OsiId == 0)
                    throw new Exception("Не указан ОСИ");
                await osiAccountSvc.CheckCanAdd(new()
                {
                    Account = request.Account,
                    AccountTypeCode = request.AccountTypeCode,
                    BankBic = request.BankBic,
                    OsiId = request.OsiId,
                    ServiceGroupId = request.ServiceGroupId,
                });
            }
            else if (request.ApplicationType == "UPDATE")
            {
                if (!request.OsiAccountId.HasValue || request.OsiAccountId.Value == 0)
                    throw new Exception("Не указан счет ОСИ");
                osiAccount = await osiAccountSvc.GetOsiAccountById(request.OsiAccountId.Value);
                if (osiAccount.OsiId != request.OsiId)
                    throw new Exception("Счет не принадлежит ОСИ");
                await osiAccountSvc.CheckCanUpdate(osiAccount, new()
                {
                    Account = request.Account,
                    AccountTypeCode = request.AccountTypeCode,
                    BankBic = request.BankBic,
                    OsiId = request.OsiId,
                    ServiceGroupId = request.ServiceGroupId,
                });
            }
            //else if (request.ApplicationType == "DELETE")
            //{
            //    if (!request.OsiAccountId.HasValue || request.OsiAccountId.Value == 0)
            //        throw new Exception("Не указан счет ОСИ");
            //    osiAccount = await osiAccountSvc.GetOsiAccountById(request.OsiAccountId.Value);
            //    if (osiAccount.OsiId != request.OsiId)
            //        throw new Exception("Счет не принадлежит ОСИ");
            //    await osiAccountSvc.CheckCanDelete(osiAccount);
            //}
            else
            {
                throw new Exception("Указан неверный тип заявки");
            }

            foreach (var doc in request.Docs)
            {
                if ((request.AccountTypeCode == Models.Enums.AccountTypeCodes.CURRENT && doc.DocTypeCode != "CURRENT_IBAN_INFO")
                || (request.AccountTypeCode == Models.Enums.AccountTypeCodes.SAVINGS && doc.DocTypeCode != "SAVING_IBAN_INFO"))
                    throw new Exception("Указан неверный тип документа");
            }

            using var db = OSIBillingDbContext.DbContext;
            var osiAccountApplication = new OsiAccountApplication
            {
                CreateDt = DateTime.Now,
                ApplicationType = request.ApplicationType,
                OsiId = request.OsiId,
                OsiAccountId = request.OsiAccountId,
                AccountTypeCode = request.AccountTypeCode,
                OldAccount = osiAccount?.Account,
                OldBankBic = osiAccount?.BankBic,
                Account = request.Account,
                BankBic = request.BankBic,
                ServiceGroupId = request.ServiceGroupId,
                State = "CREATED",
            };
            db.OsiAccountApplications.Add(osiAccountApplication);
            var sendNotification = request.Docs?.Any() == true;
            if (sendNotification)
            {
                foreach (var doc in request.Docs)
                {
                    string fileName = "account_application_" + osiAccountApplication.OsiId + "_" + doc.DocTypeCode + "_" + DateTime.Now.Ticks.ToString() + "." + doc.Extension.Replace(".", "");
                    var scan = await scanSvc.SaveDataToFile(fileName, doc.Data);

                    var dbdoc = new OsiAccountApplicationDoc
                    {
                        DocTypeCode = doc.DocTypeCode,
                        OsiAccountApplication = osiAccountApplication,
                        ScanId = scan.Id
                    };
                    db.OsiAccountApplicationDocs.Add(dbdoc);
                }
                osiAccountApplication.State = "PENDING";
            }
            await db.SaveChangesAsync();

            if (sendNotification)
                _ = telegramBotSvc.SendOsiAccountApplicationNotification(osiAccountApplication);

            return await GetOsiAccountApplicationById(osiAccountApplication.Id);
        }

        public async Task<OsiAccountApplicationDoc> AddDoc(int id, AddScanDoc request)
        {
            using var db = OSIBillingDbContext.DbContext;
            var application = await db.OsiAccountApplications.AsTracking().FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Заявка не найдена");
            if (application.State != "CREATED")
                throw new Exception("Нельзя добавить документ к заявке в статусе " + application.StateText);
            if ((application.AccountTypeCode == Models.Enums.AccountTypeCodes.CURRENT && request.DocTypeCode != "CURRENT_IBAN_INFO")
                || (application.AccountTypeCode == Models.Enums.AccountTypeCodes.SAVINGS && request.DocTypeCode != "SAVING_IBAN_INFO"))
                throw new Exception("Указан неверный тип документа");

            string fileName = "account_application_" + application.OsiId + "_" + request.DocTypeCode + "_" + DateTime.Now.Ticks.ToString() + "." + request.Extension.Replace(".", "");
            var scan = await scanSvc.SaveDataToFile(fileName, request.Data);

            var doc = new OsiAccountApplicationDoc
            {
                DocTypeCode = request.DocTypeCode,
                OsiAccountApplicationId = id,
                ScanId = scan.Id
            };
            db.OsiAccountApplicationDocs.Add(doc);
            var sendNotification = application.State != "PENDING";
            if (sendNotification)
                application.State = "PENDING";
            await db.SaveChangesAsync();

            if (sendNotification)
                _ = telegramBotSvc.SendOsiAccountApplicationNotification(application);

            doc.Scan = scan;
            doc.DocType = await db.DocTypes.FirstOrDefaultAsync(d => d.Code == doc.DocTypeCode);
            return doc;
        }

        public async Task DeleteDoc(int id, int docId)
        {
            using var db = OSIBillingDbContext.DbContext;
            var application = await db.OsiAccountApplications.AsTracking().FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Заявка не найдена");
            if (application.State != "PENDING")
                throw new Exception("Нельзя удалить документ у заявки в статусе " + application.StateText);

            var doc = await db.OsiAccountApplicationDocs.FirstOrDefaultAsync(x => x.Id == docId && x.OsiAccountApplicationId == id)
                ?? throw new Exception("Документ не найден");

            db.OsiAccountApplicationDocs.Remove(doc);
            if (await db.OsiAccountApplicationDocs.CountAsync(x => x.OsiAccountApplicationId == id) == 0)
                application.State = "CREATED";
            await scanSvc.DeleteScanById(doc.ScanId);
            await db.SaveChangesAsync();
        }

        public async Task Approve(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var application = await db.OsiAccountApplications
                .AsTracking()
                .Include(x => x.OsiAccount)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Заявка не найдена");
            if (application.State != "PENDING")
                throw new Exception("Нельзя одобрить заявку в статусе " + application.StateText);

            if (application.ApplicationType == "ADD")
            {
                await osiAccountSvc.AddOrUpdateOsiAccount(0, new OsiAccountRequest
                {
                    OsiId = application.OsiId,
                    AccountTypeCode = application.AccountTypeCode,
                    BankBic = application.BankBic,
                    Account = application.Account,
                    ServiceGroupId = application.ServiceGroupId,
                });
            }
            else if (application.ApplicationType == "UPDATE")
            {
                await osiAccountSvc.AddOrUpdateOsiAccount(application.OsiAccountId.Value, new OsiAccountRequest
                {
                    OsiId = application.OsiId,
                    AccountTypeCode = application.AccountTypeCode,
                    BankBic = application.BankBic,
                    Account = application.Account,
                    ServiceGroupId = application.ServiceGroupId,
                });
            }
            //else if (application.ApplicationType == "DELETE")
            //{
            //    await osiAccountSvc.DeleteOsiAccount(application.OsiAccountId.Value);
            //}

            application.State = "APPROVED";
            await db.SaveChangesAsync();
        }

        public async Task Reject(int id, string reason)
        {
            using var db = OSIBillingDbContext.DbContext;
            var application = await db.OsiAccountApplications
                .AsTracking()
                .Include(x => x.OsiAccount)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new Exception("Заявка не найдена");
            if (application.State != "PENDING")
                throw new Exception("Нельзя отклонить заявку в статусе " + application.StateText);

            application.State = "REJECTED";
            application.RejectReason = reason;
            await db.SaveChangesAsync();
        }
    }
}
