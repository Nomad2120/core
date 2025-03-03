using ESoft.CommonLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OSI.Core.Comparer;
using OSI.Core.Helpers;
using OSI.Core.Logic;
using OSI.Core.Logic.BankStatementParsing;
using OSI.Core.Models.AccountReports;
using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using OSI.Core.Models.Requests;
using OSI.Core.Models.Responses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IAccountReportSvc : IModelService<OSIBillingDbContext, AccountReport>
    {
        Task<AccountReport> GetAccountReport(int id);
        Task<IEnumerable<AccountReport>> GetAccountReports(int osiId);
        Task<AccountReportStatusResponse> GetPrevMonthAccountReportStatus(int osiId);
        Task<AccountReport> CreateAccountReport(AccountReportRequest request);
        Task AddList(OsiAccount account, DateTime period);
        Task FillList(int listId, byte[] fileContents);
        Task<AccountReportList> GetList(int listId);
        Task<IEnumerable<AccountReportUpdateListDetailsItem>> UpdateListDetails(int listId, IEnumerable<AccountReportUpdateListDetailsItem> items);
        Task PublishAccountReport(int reportId, AccountReportPublishRequest request);

        Task<IEnumerable<AccountReportCategoryResponse>> GetCategories();
        Task<AccountReportFormData> GetMonthlyReportFormData(int id, AccountReportPublishRequest request);
    }

    public class AccountReportSvc : ModelService<OSIBillingDbContext, AccountReport>, IAccountReportSvc
    {
        private readonly IOsiSvc osiSvc;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration;
        private readonly IScanSvc scanSvc;
        private readonly JsonSerializerOptions generatePdfJsonSerializerOptions;

        public AccountReportSvc(IOsiSvc osiSvc, IHttpClientFactory httpClientFactory, IConfiguration configuration, IScanSvc scanSvc)
        {
            this.osiSvc = osiSvc;
            this.httpClientFactory = httpClientFactory;
            this.configuration = configuration;
            this.scanSvc = scanSvc;
            generatePdfJsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
        }

        public async Task<AccountReport> GetAccountReport(int id)
        {
            using var db = DbContext;
            return await db.AccountReports
                .Include(r => r.ListRelations)
                .ThenInclude(lr => lr.List)
                .ThenInclude(l => l.Items)
                .ThenInclude(i => i.Details)
                .Include(r => r.ListRelations)
                .ThenInclude(lr => lr.List)
                .ThenInclude(l => l.AccountType)
                .Include(r => r.ListRelations)
                .ThenInclude(lr => lr.List)
                .ThenInclude(l => l.Bank)
                .Include(r => r.ListRelations)
                .ThenInclude(lr => lr.List)
                .ThenInclude(l => l.Items)
                .ThenInclude(i => i.OperationType)
                .Where(r => r.Id == id)
                .FirstOrDefaultAsync()
                ?? throw new Exception("Отчет не найден");
        }

        public async Task<IEnumerable<AccountReport>> GetAccountReports(int osiId)
        {
            var currentMonth = DateTime.Today.AddDays(1 - DateTime.Today.Day);
            using var db = DbContext;
            return await db.AccountReports
                .Include(r => r.ListRelations)
                .ThenInclude(lr => lr.List)
                .ThenInclude(l => l.AccountType)
                .Include(r => r.ListRelations)
                .ThenInclude(lr => lr.List)
                .ThenInclude(l => l.Bank)
                .Include(r => r.Docs)
                .ThenInclude(d => d.Scan)
                .Include(r => r.Docs)
                .ThenInclude(d => d.DocType)
                .Where(r => r.OsiId == osiId && r.Period < currentMonth)
                .ToListAsync();
        }

        public async Task<AccountReportStatusResponse> GetPrevMonthAccountReportStatus(int osiId)
        {
            var prevMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
            using var db = DbContext;
            return await db.AccountReports
                .Include(r => r.ListRelations)
                .ThenInclude(lr => lr.List)
                .ThenInclude(l => l.AccountType)
                .Include(r => r.ListRelations)
                .ThenInclude(lr => lr.List)
                .ThenInclude(l => l.Bank)
                .Where(r => r.OsiId == osiId && r.Period == prevMonth)
                .Select(r => new AccountReportStatusResponse
                {
                    Id = r.Id,
                    Period = r.Period,
                    State = r.State,
                    Lists = r.ListRelations.Select(lr => lr.List).Select(l => new AccountReportListStatusResponse
                    {
                        Id = l.Id,
                        Account = l.Account,
                        AccountTypeCode = l.AccountTypeCode,
                        Bic = l.Bic,
                        Bank = l.Bank,
                        IsFilled = l.IsFilled,
                        AccountType = l.AccountType,
                    }),
                })
                .FirstOrDefaultAsync()
                ?? throw new Exception("Отчет не найден");
        }

        public async Task<AccountReport> CreateAccountReport(AccountReportRequest request)
        {
            using var db = DbContext;
            await osiSvc.CheckOsiById(request.OsiId);
            var period = new DateTime(request.Period.Year, request.Period.Month, 1);
            if (await db.AccountReports.AnyAsync(r => r.OsiId == request.OsiId && r.Period == period))
                throw new Exception("Отчет за данный период уже создан");
            var accounts = await db.OsiAccounts.Where(s => s.OsiId == request.OsiId).ToListAsync();
            var report = new AccountReport
            {
                OsiId = request.OsiId,
                Period = period,
            };
            var filled = true;
            foreach (var account in accounts)
            {
                var list = await db.AccountReportLists.AsTracking().FirstOrDefaultAsync(l => l.Account == account.Account && l.AccountTypeCode == account.AccountTypeCode && l.Period == period);

                if (list == null)
                {
                    list = new()
                    {
                        Account = account.Account,
                        AccountTypeCode = account.AccountTypeCode,
                        Bic = account.BankBic,
                        Period = period,
                        IsFilled = false,
                    };
                    db.AccountReportLists.Add(list);
                }

                filled &= list.IsFilled;

                report.ListRelations.Add(new() { Report = report, List = list });
            }
            report.State = !filled ? AccountReportStateCodes.CREATED : AccountReportStateCodes.FILLED;
            db.AccountReports.Add(report);
            await db.SaveChangesAsync();
            return report;
        }

        public async Task AddList(OsiAccount account, DateTime period)
        {
            using var db = DbContext;
            period = new DateTime(period.Year, period.Month, 1);

            var report = await db.AccountReports.FirstOrDefaultAsync(r => r.OsiId == account.OsiId && r.Period == period);
            if (report != null)
            {

                var list = await db.AccountReportLists.AsTracking().FirstOrDefaultAsync(l => l.Account == account.Account && l.AccountTypeCode == account.AccountTypeCode && l.Period == period);

                if (list == null)
                {
                    list = new()
                    {
                        Account = account.Account,
                        AccountTypeCode = account.AccountTypeCode,
                        Bic = account.BankBic,
                        Period = period,
                        IsFilled = false,
                    };
                    db.AccountReportLists.Add(list);
                }
                else
                {
                    if (await db.AccountReportListRelations.AnyAsync(lr => lr.ReportId == report.Id && lr.ListId == list.Id))
                        return;
                }

                db.AccountReportListRelations.Add(new()
                {
                    ReportId = report.Id,
                    List = list
                });
                await db.SaveChangesAsync();
            }
            else
            {
                await CreateAccountReport(new() { OsiId = account.OsiId, Period = period });
            }
        }

        public async Task FillList(int listId, byte[] fileContents)
        {
            using var db = DbContext;
            var dbList = await db.AccountReportLists
                .AsTracking()
                .Include(l => l.Relations)
                .ThenInclude(lr => lr.Report)
                .FirstOrDefaultAsync(l => l.Id == listId)
                ?? throw new Exception("Счет не найден");
            if (dbList.IsFilled)
                throw new Exception("Выписка по счету уже прикреплялась");
            if (dbList.Reports.Any(r => r.State == AccountReportStateCodes.PUBLISHED))
                throw new Exception("Нельзя изменять опубликованный отчет");
            //var bankStatement = new BankStatement
            //{
            //    Account = dbList.Account,
            //    Bic = dbList.Bic,
            //    PeriodStart = dbList.Report.Period,
            //};
            IStatementParser parser = dbList.Bic switch
            {
                "HSBKKZKX" => new HalykBankStatementParser(),
                "CASPKZKA" => new KaspiBankStatementParser(),
                "KCJBKZKX" => new BccBankTxtStatementParser(),
                "TSESKZKA" => new JusanBankStatementParser(),
                "KPSTKZKA" => new KazpostStatementParser(),
                "KSNVKZKA" => new FreedomBankStatementParser(),
                _ => throw new Exception("Для данного банка пока не реализована загрузка выписки"),
            };
            BankStatement bankStatement = ParseFileContents(parser, fileContents);
            if (bankStatement != null)
            {
                if (bankStatement.Account != dbList.Account)
                    throw new Exception($"Счет в выписке '{bankStatement.Account}' не совпадает с указанным '{dbList.Account}'");

                if (bankStatement.PeriodStart != dbList.Period)
                    throw new Exception($"Дата начала выписки '{bankStatement.PeriodStart.ToString("dd-MM-yyyy")}' не совпадает с указанной '{dbList.Period.ToString("dd-MM-yyyy")}'");

                if (bankStatement.PeriodEnd != dbList.Period.AddMonths(1).AddDays(-1))
                    throw new Exception($"Дата конца выписки '{bankStatement.PeriodEnd.ToString("dd-MM-yyyy")}' не совпадает с указанной '{dbList.Period.AddMonths(1).AddDays(-1).ToString("dd-MM-yyyy")}'");

                // БИК в выписке отсутствует, только в деталях у получателя или отправителя, зависит от операции
                bankStatement.Bic = dbList.Bic;
            }
            else
            {
                throw new Exception("Выписка не загружена");
            }
            foreach (var item in bankStatement.Items)
            {
                int? categoryId = item.OperationTypeCode switch
                {
                    OperationTypeCodes.KREDIT => int.TryParse(Regex.Match(item.Assign, "#GP_(\\d+)").Groups[1].Value, out var serviceGroupId)
                        ? serviceGroupId switch
                        {
                            1 or 5 or 6 => 6, //6.1
                            4 => 26, //6.2
                            2 => 7, //6.3
                            7 => 8, //6.4
                            3 => 9, //6.5
                            _ => null,
                        }
                        : null,
                    OperationTypeCodes.DEBET => item.SenderBin == item.ReceiverBin
                        ? 28 //7.7
                        : null,
                    _ => null,
                };
                dbList.Items.Add(new()
                {
                    Dt = item.Dt,
                    Amount = item.Amount,
                    Receiver = item.Receiver,
                    ReceiverBin = item.ReceiverBin,
                    Sender = item.Sender,
                    SenderBin = item.SenderBin,
                    Assign = item.Assign,
                    OperationTypeCode = item.OperationTypeCode,
                    CategoryId = categoryId,
                });
            }

            // OSI-547 комиссии теперь не добавляем в отчет
            /*
            var comissions = await db.PaymentOrders
                .Where(po => po.Account == dbList.Account &&
                             po.DtReg >= dbList.Period &&
                             po.DtReg < dbList.Period.AddMonths(1))
                .If(dbList.AccountTypeCode == AccountTypeCodes.SAVINGS)
                .Where(po => po.ServiceGroupId == 2)
                .Else()
                .Where(po => po.ServiceGroupId != 2)
                .EndIf()
                .Select(po => new { po.Bic, po.Contract.BankName, po.ComisBank, po.ComisOur, OsiName = po.Osi.Name, OsiIdn = po.Osi.Idn })
                .ToListAsync();
            foreach (var comissionsByOsi in comissions.GroupBy(c => new { c.OsiName, c.OsiIdn }))
            {
                var osiName = comissionsByOsi.Key.OsiName;
                var osiIdn = comissionsByOsi.Key.OsiIdn;
                var comisOurTotal = comissionsByOsi.Sum(c => c.ComisOur);
                if (comisOurTotal > 0)
                {
                    dbList.Items.Add(new()
                    {
                        Dt = dbList.Period.AddMonths(1).AddDays(-1),
                        Amount = comisOurTotal,
                        Receiver = PaymentOrderSvc.OurName,
                        ReceiverBin = PaymentOrderSvc.OurIdn,
                        Sender = osiName,
                        SenderBin = osiIdn,
                        Assign = PaymentOrderSvc.OurAssign,
                        OperationTypeCode = OperationTypeCodes.DEBET,
                        CategoryId = 13, // 7.1.1
                    });
                }
                foreach (var comissionsByBank in comissionsByOsi.GroupBy(c => new { c.Bic, c.BankName }))
                {
                    var comisBank = comissionsByBank.Sum(c => c.ComisBank);
                    if (comisBank > 0)
                    {
                        var bankName = comissionsByBank.Key.BankName;
                        dbList.Items.Add(new()
                        {
                            Dt = dbList.Period.AddMonths(1).AddDays(-1),
                            Amount = comisBank,
                            Receiver = bankName,
                            ReceiverBin = string.Empty,
                            Sender = osiName,
                            SenderBin = osiIdn,
                            Assign = $"Комиссия банка {bankName}",
                            OperationTypeCode = OperationTypeCodes.DEBET,
                            CategoryId = 16, // 7.1.4
                        });
                    }
                }
            }
            */

            dbList.Begin = bankStatement.Begin;
            dbList.Debet = bankStatement.Debet;
            dbList.Kredit = bankStatement.Kredit;
            dbList.End = bankStatement.End;
            dbList.IsFilled = true;
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var reportIds = dbList.Reports.Select(r => r.Id).ToArray();
            var reports = await db.AccountReports.Include(r => r.ListRelations).ThenInclude(lr => lr.List).Where(r => r.Id.In(reportIds)).ToListAsync();
            foreach (var report in reports)
            {
                if (report.Lists.All(l => l.IsFilled))
                {
                    report.State = AccountReportStateCodes.FILLED;
                    db.Entry(report).Property(r => r.State).IsModified = true;
                }
            }
            await db.SaveChangesAsync();
        }

        public async Task<AccountReportList> GetList(int listId)
        {
            using var db = DbContext;
            var list = await db.AccountReportLists
                .Include(l => l.AccountType)
                .Include(l => l.Bank)
                .Include(l => l.Items)
                .ThenInclude(i => i.Details)
                .Include(l => l.Items)
                .ThenInclude(i => i.OperationType)
                .FirstOrDefaultAsync(l => l.Id == listId)
                ?? throw new Exception("Счет не найден");
            list.IsInPublishedReport = await db.AccountReportListRelations.AnyAsync(lr => lr.ListId == list.Id && lr.Report.State == AccountReportStateCodes.PUBLISHED);
            return list;
        }

        private static BankStatement ParseFileContents(IStatementParser parser, byte[] fileContents)
        {
            if (!parser.CheckFileFormat(fileContents))
            {
                throw new Exception("Формат файла не соответствует требуемому");
            }
            BankStatement bankStatement = parser.ParseData(fileContents);

            return bankStatement;
        }

        public async Task<IEnumerable<AccountReportUpdateListDetailsItem>> UpdateListDetails(int listId, IEnumerable<AccountReportUpdateListDetailsItem> items)
        {
            if (!items.Any())
            {
                return items;
            }

            var result = new List<AccountReportUpdateListDetailsItem>();

            using var db = DbContext;
            var list = await db.AccountReportLists
                .AsTracking()
                .Include(l => l.Items)
                .ThenInclude(i => i.Details)
                .FirstOrDefaultAsync(l => l.Id == listId)
                ?? throw new Exception("Счет не найден");
            if (await db.AccountReportLists
                .Where(l => l.Id == listId)
                .AnyAsync(l => l.Relations.Select(lr => lr.Report).Any(r => r.State == AccountReportStateCodes.PUBLISHED)))
                throw new Exception("Нельзя изменять опубликованный отчет");

            foreach (var item in items)
            {
                var dbItem = list.Items.FirstOrDefault(i => i.Id == item.Id)
                    ?? throw new Exception($"Не найдена запись с id {item.Id}");

                if (item.Details == null)
                    throw new Exception("Не передан список комментариев");

                var duplicateDetails = item.Details.Where(d => d.Id != 0).GroupBy(d => d.Id).FirstOrDefault(g => g.Count() > 1);
                if (duplicateDetails != null)
                    throw new Exception($"Повторяются комментарии с id {duplicateDetails.Key}");

                //OSI-414: проверка суммы details с суммой item
                if (item.Details.Any())
                {
                    var detailsTotalAmount = item.Details.Sum(d => d.Amount);
                    if (dbItem.Amount != detailsTotalAmount)
                        throw new Exception($"Сумма расшифровок {detailsTotalAmount.ToString("F2").Replace(",", ".")} не совпадает с суммой проводки {dbItem.Amount.ToString("F2").Replace(",", ".")}");
                }

                var resultItemDetails = new List<AccountReportListItemDetail>();
                foreach (var detail in item.Details)
                {
                    var dbDetail = dbItem.Details.SingleOrDefault(d => d.Id == detail.Id);
                    if (dbDetail == null)
                    {
                        if (detail.Id == 0)
                        {
                            dbDetail = new();
                        }
                        else
                            throw new Exception($"Не найден комментарий с id {detail.Id}");
                    }
                    dbDetail.Amount = detail.Amount;
                    dbDetail.Comment = detail.Comment;
                    dbDetail.CategoryId = detail.CategoryId;
                    resultItemDetails.Add(dbDetail);
                }
                dbItem.Details.Where(d => !resultItemDetails.Contains(d)).ToList().ForEach(d => db.AccountReportListItemDetails.Remove(d));
                resultItemDetails.Where(d => !dbItem.Details.Contains(d)).ToList().ForEach(d => dbItem.Details.Add(d));
                dbItem.CategoryId = resultItemDetails.Count > 0 ? null : item.CategoryId;
                result.Add(new()
                {
                    Id = item.Id,
                    CategoryId = dbItem.CategoryId,
                    Details = resultItemDetails,
                });
            }

            await db.SaveChangesAsync();

            return result;
        }

        public async Task PublishAccountReport(int reportId, AccountReportPublishRequest request)
        {
            using var db = DbContext;
            var report = await db.AccountReports.AsTracking().FirstOrDefaultAsync(r => r.Id == reportId)
                ?? throw new Exception("Отчет не найден");
            if (report.State == AccountReportStateCodes.PUBLISHED)
                throw new Exception("Отчет уже опубликован");
            if (report.Period >= DateTime.Today.AddDays(1 - DateTime.Today.Day))
                throw new Exception("Период отчета еще не закончен");
            if (report.State != AccountReportStateCodes.FILLED)
                throw new Exception("Отчет не заполнен полностью");
            var lists = await db.AccountReportLists
                .AsTracking()
                .Include(l => l.Items)
                .ThenInclude(i => i.Details)
                .Where(l => l.Relations.Any(lr => lr.ReportId == reportId))
                .ToListAsync();
            if (lists.Any(l => l.Items.Any(i => i.CategoryId == null && i.Details.Count == 0)))
                throw new Exception("Не проставлены категории");

            foreach (var item in lists.SelectMany(l => l.Items).Where(i => i.CategoryId != null && i.Details.Count > 0))
            {
                item.CategoryId = null;
            }
            report.State = AccountReportStateCodes.PUBLISHED;
            report.PublishDate = DateTime.Today;

            var accountReportFormData = await GetMonthlyReportFormData(reportId, request);
            using var client = httpClientFactory.CreateClient();
            var httpResponseMessage = await client.PostAsJsonAsync(configuration["Urls:AccountReportGeneratePdf"],
                accountReportFormData, generatePdfJsonSerializerOptions);
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var fileName = "account_report_" + reportId + "_" + report.Period.ToString("yyyy-MM") + ".pdf";
                var scan = await scanSvc.SaveDataToFile(fileName,
                    Convert.FromBase64String((await httpResponseMessage.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("docBase64").GetString()));

                var doc = new AccountReportDoc
                {
                    DocTypeCode = "ACCOUNT_REPORT_MONTHLY",
                    AccountReportId = reportId,
                    ScanId = scan.Id
                };
                db.AccountReportDocs.Add(doc);
                await db.SaveChangesAsync();
            }
            else
            {
                throw new Exception("Ошибка при генерации PDF");
            }
        }

        public async Task<IEnumerable<AccountReportCategoryResponse>> GetCategories()
        {
            using var db = DbContext;
            var categories = await db.AccountReportCategoryOptions
                .Include(co => co.Category)
                .Include(co => co.AccountType)
                .Include(co => co.OperationType)
                .Select(co => new AccountReportCategoryResponse
                {
                    Id = co.CategoryId,
                    Number = co.Category.Number,
                    NameRu = co.Category.NameRu,
                    NameKz = co.Category.NameKz,
                    AccountTypeCode = co.AccountTypeCode,
                    AccountType = co.AccountType,
                    OperationTypeCode = co.OperationTypeCode,
                    OperationType = co.OperationType,
                })
                .ToListAsync();
            return categories;
        }

        public async Task<AccountReportFormData> GetMonthlyReportFormData(int id, AccountReportPublishRequest request)
        {
            using var db = DbContext;
            var report = await db.AccountReports
                .Include(r => r.ListRelations)
                .ThenInclude(lr => lr.List)
                .ThenInclude(l => l.Items)
                .ThenInclude(i => i.Details)

                .Include(r => r.ListRelations)
                .ThenInclude(lr => lr.List)
                .ThenInclude(l => l.AccountType)

                .Include(r => r.ListRelations)
                .ThenInclude(lr => lr.List)
                .ThenInclude(l => l.Bank)

                .Include(r => r.ListRelations)
                .ThenInclude(lr => lr.List)
                .ThenInclude(l => l.Items)
                .ThenInclude(i => i.OperationType)

                .Include(r => r.Osi)
                .ThenInclude(o => o.UnionType)

                .Where(r => r.Id == id)
                .FirstOrDefaultAsync()
                ?? throw new Exception("Отчет не найден");

            if (report.Osi.RegistrationType == "FREE" && request == null)
                throw new Exception("Отсутствуют суммы задолженностей по обязательным ежемесячным взносам собственников");

            var result = new AccountReportFormData()
            {
                Period = report.Period,
                OsiName = report.Osi.Name,
                OsiAddress = report.Osi.Address,
                Signer = report.Osi.Fio,
                UnionType = report.Osi.UnionType,
            };

            var allCategories = await db.AccountReportCategories.AsNoTrackingWithIdentityResolution().ToListAsync();
            var maxId = allCategories.Max(c => c.Id);

            var categoriesToDetail = GetCategoriesToDetail(allCategories, 11);
            var categoryIdsToShowWithoutNumber = new HashSet<int>();
            foreach (var category in categoriesToDetail)
            {
                foreach (var item in report.Lists.SelectMany(l => l.Items.Where(i => i.CategoryId == category.Id && i.Details.Count == 0)))
                {
                    var detailCategory = new AccountReportCategory
                    {
                        Id = ++maxId,
                        Number = $"{category.Number}.{item.Id}",
                        NameRu = $"БИН: {item.ReceiverBin}, {item.Receiver}, {item.Assign}",
                        NameKz = $"БИН: {item.ReceiverBin}, {item.Receiver}, {item.Assign}",
                    };
                    item.CategoryId = detailCategory.Id;
                    category.SubCategories.Add(detailCategory);
                    allCategories.Add(detailCategory);
                    categoryIdsToShowWithoutNumber.Add(detailCategory.Id);
                }
                foreach (var detail in report.Lists.SelectMany(l => l.Items.SelectMany(i => i.Details.Where(d => d.CategoryId == category.Id))))
                {
                    var detailCategory = new AccountReportCategory
                    {
                        Id = ++maxId,
                        Number = $"{category.Number}.{detail.ItemId}.{detail.Id}",
                        NameRu = $"БИН: {detail.Item.ReceiverBin}, {detail.Item.Receiver}, {detail.Comment}",
                        NameKz = $"БИН: {detail.Item.ReceiverBin}, {detail.Item.Receiver}, {detail.Comment}",
                    };
                    detail.CategoryId = detailCategory.Id;
                    category.SubCategories.Add(detailCategory);
                    allCategories.Add(detailCategory);
                    categoryIdsToShowWithoutNumber.Add(detailCategory.Id);
                }
            }

            var resultCategories = allCategories
                .OrderBy(c => c.Number, NaturalComparer.Instance)
                .ToDictionary(c => c.Id, c => new AccountReportCategoryFormData
                {
                    Number = categoryIdsToShowWithoutNumber.Contains(c.Id) ? "" : c.Number,
                    NameRu = c.NameRu,
                    NameKz = c.NameKz,
                    Amount = 0,
                });

            resultCategories[1].Amount = report.Lists.Where(l => l.AccountTypeCode == AccountTypeCodes.CURRENT).Sum(l => l.End);
            resultCategories[2].Amount = report.Lists.Where(l => l.AccountTypeCode == AccountTypeCodes.SAVINGS).Sum(l => l.End);

            var osvDate = report.Period.AddMonths(1).AddDays(-1);
            if (report.Osi.RegistrationType != "FREE")
            {
                var osv = await OSVLogic.GetOSVOnDateByOsi(osvDate, osvDate, report.Osi);
                resultCategories[3].Amount =
                    osv.Abonents.Sum(a => a.Services.Where(s => !s.ServiceGroupId.In(2, 4) && s.End > 0).Select(s => s.End).DefaultIfEmpty().Sum());
                resultCategories[4].Amount =
                    osv.Abonents.Sum(a => a.Services.Where(s => s.ServiceGroupId == 2 && s.End > 0).Select(s => s.End).DefaultIfEmpty().Sum());
                resultCategories[25].Amount =
                    osv.Abonents.Sum(a => a.Services.Where(s => s.ServiceGroupId == 4 && s.End > 0).Select(s => s.End).DefaultIfEmpty().Sum());
            }
            else
            {
                resultCategories[3].Amount = request.MaintenanceAmount;
                resultCategories[4].Amount = request.SavingsAmount;
                resultCategories[25].Amount = request.ParkingAmount;
            }

            foreach (var list in report.Lists)
            {
                foreach (var item in list.Items)
                {
                    if (item.Details.Count > 0)
                    {
                        foreach (var detail in item.Details.Where(d => d.CategoryId.HasValue))
                        {
                            resultCategories[detail.CategoryId.Value].Amount += detail.Amount;
                        }
                    }
                    else if (item.CategoryId.HasValue)
                    {
                        resultCategories[item.CategoryId.Value].Amount += item.Amount;
                    }
                }
            }

            SumSubCategoriesAmounts(allCategories.Where(c => c.ParentId is null), resultCategories);

            result.Categories = resultCategories.Values.ToList();

            return result;
        }

        private static IEnumerable<AccountReportCategory> GetCategoriesToDetail(IEnumerable<AccountReportCategory> categories, int? parentId)
        {
            var categoriesToDetail = new List<AccountReportCategory>();
            foreach (var category in categories.Where(c => c.ParentId == parentId))
            {
                if (category.SubCategories.Count > 0)
                {
                    categoriesToDetail.AddRange(GetCategoriesToDetail(categories, category.Id));
                }
                else
                {
                    categoriesToDetail.Add(category);
                }
            }
            return categoriesToDetail;
        }

        private static void SumSubCategoriesAmounts(IEnumerable<AccountReportCategory> categories, Dictionary<int, AccountReportCategoryFormData> resultCategories)
        {
            foreach (var category in categories)
            {
                if (category.SubCategories.Count > 0)
                {
                    SumSubCategoriesAmounts(category.SubCategories, resultCategories);
                    resultCategories[category.Id].Amount = 0;
                    foreach (var subCategory in category.SubCategories)
                    {
                        resultCategories[category.Id].Amount += resultCategories[subCategory.Id].Amount;
                    }
                }
            }
        }
    }
}
