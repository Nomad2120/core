using ESoft.CommonLibrary;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OSI.Core.Logic;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using OSI.Core.Models.Reports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Web;

namespace OSI.Core.Services
{
    public interface IPrintInvoiceSvc
    {
        Task<ApiResponse<PrintInvoicesResult>> GetInvoicesByOsiIdOnCurrentDate(bool isStraightOrder, bool isOnlyResidents, int osiId);
        Task<ApiResponse<PrintInvoicesResult>> GetInvoicesByOsiIdOnDate(bool isStraightOrder, bool isOnlyResidents, int osiId, int year, int month);
        Task<ApiResponse<string>> GetInvoicesByAllOsiOnDate(bool isStraightOrder, bool isOnlyResidents, int year, int month);
        Task<ApiResponse<byte[]>> GetInvoiceByAbonentIdOnCurrentDate(int abonentId);
        Task<ApiResponse<byte[]>> GetInvoiceByAbonentIdsOnCurrentDate(bool isStraightOrder, bool isOnlyResidents, IEnumerable<int> abonentId);
        Task<ApiResponse<byte[]>> GetInvoiceByAbonentIdOnDate(int abonentId, int year, int month);
        Task<ApiResponse<byte[]>> GetInvoiceByAbonentIdsOnDate(bool isStraightOrder, bool isOnlyResidents, IEnumerable<int> abonentId, int year, int month);
        //Task<ApiResponse<string>> GetCountInvoicesByAllOsiOnDate(int year, int month);
    }

    public class PrintInvoiceSvc : IPrintInvoiceSvc
    {
        public string InvoicesFolder => Path.Combine(env.WebRootPath, "invoices");

        private const string KaspiUrlQR = "https://kaspi.kz/pay/NurTau?5742=";

        private readonly ITransactionSvc transactionSvc;
        private readonly IWebHostEnvironment env;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration;
        private readonly IQRCodeSvc qrCodeSvc;
        private readonly ILogger<PrintInvoiceSvc> logger;

        public PrintInvoiceSvc(
            ITransactionSvc transactionSvc,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IQRCodeSvc qrCodeSvc,
            ILogger<PrintInvoiceSvc> logger)
        {
            this.transactionSvc = transactionSvc;
            this.env = env;
            this.httpClientFactory = httpClientFactory;
            this.configuration = configuration;
            this.qrCodeSvc = qrCodeSvc;
            this.logger = logger;
            Directory.CreateDirectory(InvoicesFolder);
        }

        public Task<ApiResponse<byte[]>> GetInvoiceByAbonentIdOnCurrentDate(int abonentId)
        {
            return GetInvoiceByAbonentIdsOnDate(true, false, new[] { abonentId }, DateTime.Today.Year, DateTime.Today.Month);
        }

        public Task<ApiResponse<byte[]>> GetInvoiceByAbonentIdsOnCurrentDate(bool isStraightOrder, bool isOnlyResidents, IEnumerable<int> abonentIds)
        {
            return GetInvoiceByAbonentIdsOnDate(isStraightOrder, isOnlyResidents, abonentIds, DateTime.Today.Year, DateTime.Today.Month);
        }

        public Task<ApiResponse<byte[]>> GetInvoiceByAbonentIdOnDate(int abonentId, int year, int month)
        {
            return GetInvoiceByAbonentIdsOnDate(true, false, new[] { abonentId }, year, month);
        }

        public async Task<ApiResponse<byte[]>> GetInvoiceByAbonentIdsOnDate(bool isStraightOrder,
                                                                            bool isOnlyResidents,
                                                                            IEnumerable<int> abonentIds,
                                                                            int year,
                                                                            int month)
        {
            ApiResponse<byte[]> apiResponse = new();
            using var db = OSIBillingDbContext.DbContext;
            var abonents = await db.Abonents.Where(a => abonentIds.Contains(a.Id)).ToListAsync();
            if (abonents?.Any() != true)
            {
                apiResponse.Code = -1;
                string pluralEnd = abonentIds.Count() == 1 ? "" : "ы";
                apiResponse.Message = $"Абонент{pluralEnd} не найден{pluralEnd}";
                return apiResponse;
            }

            int osiId;
            if (abonentIds.Count() == 1)
            {
                osiId = abonents.First().OsiId;
            }
            else
            {
                var osies = abonents.GroupBy(a => a.OsiId);
                if (osies.Count() > 1)
                {
                    apiResponse.Code = -1;
                    apiResponse.Message = "Допускается список абонентов только из одного ОСИ";
                    return apiResponse;
                }
                osiId = osies.First().Key;
            }
            Osi osi = await db.Osies.FirstOrDefaultAsync(o => o.Id == osiId);
            if (osi == null)
            {
                apiResponse.Code = -1;
                apiResponse.Message = "ОСИ не найден";
                return apiResponse;
            }

            var htmlContentApiResponse = await GetInvoicesHtml(isStraightOrder,
                                                               isOnlyResidents,
                                                               osi,
                                                               year,
                                                               month,
                                                               accuralsPredicate: t => abonentIds.Contains(t.AbonentId),
                                                               GetOSV: (dateBegin, dateEnd) => transactionSvc.GetOSVOnDateByAbonents(dateBegin, dateEnd, abonents));
            if (htmlContentApiResponse.Code != 0)
            {
                apiResponse.From(htmlContentApiResponse);
                return apiResponse;
            }
            string htmlContent = htmlContentApiResponse.Result.Data;

            // преобразуем в pdf
            byte[] pdf;
            using HttpClient client = httpClientFactory.CreateClient();
            string requestUri = configuration["Urls:CefSharpApi"] + $"Print/pdf";
            var httpResponseMessage = await client.PostAsync(requestUri, new StringContent(htmlContent));
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var response = await httpResponseMessage.Content.ReadFromJsonAsync<ApiResponse<string>>();
                if (response.Code == 0)
                {
                    pdf = Convert.FromBase64String(response.Result);
                }
                else
                {
                    apiResponse.From(response);
                    apiResponse.Code = -3;
                    return apiResponse;
                }
            }
            else
            {
                apiResponse.Code = -2;
                apiResponse.Message = "Ошибка при соединении с CefSharp API";
                return apiResponse;
            }

            apiResponse.Result = pdf;
            return apiResponse;
        }

        private async Task<ApiResponse<PrintInvoicesResult>> GetInvoicesHtml(bool isStraightOrder,
                                                                bool isOnlyResidents,
                                                                Osi osi,
                                                                int year,
                                                                int month,
                                                                Expression<Func<Transaction, bool>> accuralsPredicate,
                                                                Func<DateTime, DateTime, Task<OSV>> GetOSV)
        {
            ApiResponse<PrintInvoicesResult> apiResponse = new();
            using var db = OSIBillingDbContext.DbContext;
            DateTime dateToFind = new DateTime(year, month, 1);

            PlanAccural planAccural = await db.PlanAccurals.FirstOrDefaultAsync(p => p.OsiId == osi.Id && p.BeginDate == dateToFind);
            if (planAccural != null)
            {
                if (!planAccural.AccuralCompleted)
                {
                    apiResponse.Code = -1;
                    apiResponse.Message = "За указанный период начисления еще не производились";
                    return apiResponse;
                }
            }
            else
            {
                apiResponse.Code = -1;
                apiResponse.Message = "За указанный период не найден план начислений";
                return apiResponse;
            }

            DateTime dateBegin = planAccural.BeginDate;
            DateTime dateEnd = DateTime.Today;

            // за предыдущие месяцы например
            DateTime firstDayOfCurrentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            if (planAccural.BeginDate != firstDayOfCurrentMonth)
            {
                // последнее число того месяца
                dateEnd = planAccural.BeginDate.AddMonths(1).AddDays(-1);
            }

            var OSV = await GetOSV(dateBegin, dateEnd);
            if (!OSV.Abonents.Any())
            {
                apiResponse.Code = -1;
                apiResponse.Message = "Начислений нет. Квитанции не сформированы";
                return apiResponse;
            }

            // OSI-230, 19-02-2023, shuma
            //var OSVAccurals = await OSVLogic.GetAccuralsOnPeriodByServices(dateBegin, dateEnd, null, osi);
            var accurals = await GetAbonentsAccuralsOnPeriod(dateBegin, dateEnd, accuralsPredicate);

            string period = DateTime.Today.ToString("dd.MM.yyyy");

            // сортируем по кв.
            // OSI-268 сортировка помещений с natural comparer делается внутри методов получения порядка абонентов GetStraight... и GetThrough...
            // получим html и запишем в файл
            var printResult = await PrintInvoiceLogic.GetInvoicesByListOfAbonents(isStraightOrder,
                                                                            isOnlyResidents,
                                                                            OSV.Abonents,
                                                                            accurals,
                                                                            osi.Name,
                                                                            osi.Address,
                                                                            period,
                                                                            s => qrCodeSvc.GetQRCodeBase64(3, KaspiUrlQR + s));

            apiResponse.Result = printResult;
            return apiResponse;
        }

        private async Task<ApiResponse<PrintInvoicesResult>> GetInvoicesByOsiAndDate(bool isStraightOrder,
                                                                        bool isOnlyResidents,
                                                                        Osi osi,
                                                                        int year,
                                                                        int month,
                                                                        string filename = null)
        {
            ApiResponse<PrintInvoicesResult> apiResponse = new();

            var htmlContentApiResponse = await GetInvoicesHtml(isStraightOrder,
                                                               isOnlyResidents,
                                                               osi,
                                                               year,
                                                               month,
                                                               accuralsPredicate: t => t.OsiId == osi.Id,
                                                               GetOSV: (dateBegin, dateEnd) => transactionSvc.GetOSVOnDateByOsi(dateBegin, dateEnd, osi));
            if (htmlContentApiResponse.Code != 0)
            {
                logger.LogError(htmlContentApiResponse.Message);
                apiResponse.From(htmlContentApiResponse);
                return apiResponse;
            }
            string htmlContent = htmlContentApiResponse.Result.Data;
            // запишем html в файл
            string htmlFilename = string.IsNullOrEmpty(filename) ? DateTime.Now.Ticks.ToString() + ".inv" : filename;
            string htmlPath = Path.Combine(InvoicesFolder, htmlFilename);
            await File.WriteAllTextAsync(htmlPath, htmlContent);

            // преобразуем в pdf
            string pdfPath;
            using HttpClient client = httpClientFactory.CreateClient();
            string requestUri = configuration["Urls:CefSharpApi"] + $"Print/pdf?htmlPath={HttpUtility.UrlEncode(Path.GetFullPath(htmlPath))}";
            var httpResponseMessage = await client.GetAsync(requestUri);
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var response = await httpResponseMessage.Content.ReadFromJsonAsync<ApiResponse<string>>();
                if (response.Code == 0)
                {
                    pdfPath = response.Result;
                }
                else
                {
                    logger.LogError(response.Message);
                    apiResponse.Result = new PrintInvoicesResult
                    {
                        Count = 0,
                        Data = response.Result
                    };
                    return apiResponse;
                }
            }
            else
            {
                apiResponse.Code = -1;
                apiResponse.Message = "Ошибка при соединении с CefSharp API";
                return apiResponse;
            }

            apiResponse.Result = new PrintInvoicesResult
            {
                Count = htmlContentApiResponse.Result.Count,
                Data = pdfPath
            };
            return apiResponse;
        }

        private static async Task<Osi> GetOsi(int osiId)
        {
            using var db = OSIBillingDbContext.DbContext;

            Osi osi = await db.Osies.FirstOrDefaultAsync(a => a.Id == osiId);
            if (osi == null)
                throw new Exception("ОСИ не найден");

            return osi;
        }

        public async Task<ApiResponse<PrintInvoicesResult>> GetInvoicesByOsiIdOnCurrentDate(bool isStraightOrder, bool isOnlyResidents, int osiId)
        {
            var osi = await GetOsi(osiId);
            var result = await GetInvoicesByOsiAndDate(isStraightOrder,
                                                       isOnlyResidents,
                                                       osi,
                                                       DateTime.Today.Year,
                                                       DateTime.Today.Month);
            return result;
        }

        public async Task<ApiResponse<PrintInvoicesResult>> GetInvoicesByOsiIdOnDate(bool isStraightOrder, bool isOnlyResidents, int osiId, int year, int month)
        {
            var osi = await GetOsi(osiId);
            var result = await GetInvoicesByOsiAndDate(isStraightOrder, isOnlyResidents, osi, year, month);
            return result;
        }

        public async Task<ApiResponse<string>> GetInvoicesByAllOsiOnDate(bool isStraightOrder, bool isOnlyResidents, int year, int month)
        {
            try
            {
                using var db = OSIBillingDbContext.DbContext;

                string result = "";
                ApiResponse<string> apiResponse = new();

                var osies = await db.Osies.Where(o => o.IsLaunched).ToListAsync();
                logger.LogInformation($"Выбраны ОСИ: {string.Join(',', osies.Select(o => o.Id.ToString()).ToArray())}");
                foreach (var osi in osies)
                {
                    string filename = osi.Id + "_" + osi.Name
                        .Replace("\"", "")
                        .Replace("\\", "-")
                        .Replace("/", "-")
                        .Replace(":", "")
                        .Replace("?", "")
                        .Replace("*", "") + " на " + DateTime.Today.ToString("dd-MM-yyyy");

                    logger.LogInformation($"Начало печати по оси ID {osi.Id}, {osi.Name}, файл '{filename}'");

                    string pdfFile = Path.Combine(InvoicesFolder, filename + ".pdf");
                    if (!File.Exists(pdfFile))
                    {
                        var r = await GetInvoicesByOsiAndDate(isStraightOrder, isOnlyResidents, osi, year, month, filename + ".inv");
                        logger.LogInformation($"Оси ID {osi.Id}, {osi.Name}: {r.Result?.Count.ToString() ?? ("0 - " + r.Message)}");
                        result += $"{osi.Id}\t{osi.Name}\t{osi.Address}\t{r.Result?.Count.ToString() ?? ("0 - " + r.Message)}\n";
                    }
                    else
                    {
                        logger.LogInformation($"ОШИБКА: файл '{pdfFile}' существует, печать отменена");
                    }
                }
                logger.LogInformation($"Завершено. Результат:");
                logger.LogInformation(result);
                apiResponse.Result = result;

                return apiResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.InnerException?.Message ?? ex.Message);
                throw;
            }
        }

        public async Task<List<AbonentAccural>> GetAbonentsAccuralsOnPeriod(DateTime onDate1,
                                                                            DateTime onDate2,
                                                                            Expression<Func<Transaction, bool>> predicate)
        {
            using var db = OSIBillingDbContext.DbContext;

            var accurals = await db.Transactions
                .Include(t => t.Group)
                .Include(t => t.OsiService)
                .Include(t => t.Abonent)
                .Include(t => t.OsiServiceAmount)
                .ThenInclude(x => x.AccuralMethod)
                .Where(t => t.Dt >= onDate1.Date && t.Dt < onDate2.Date.AddDays(1))
                .Where(t => t.TransactionType.In(TransactionTypeCodes.ACC, TransactionTypeCodes.FIX))
                //.Where(t => t.GroupId == 1)
                .Where(predicate)
                .Select(t => new AbonentAccural
                {
                    AbonentId = t.AbonentId,
                    AccuralMethodCode = t.OsiServiceAmount.AccuralMethod.Code,
                    Square = t.Abonent.Square,
                    EffectiveSquare = t.Abonent.EffectiveSquare,
                    ServiceId = t.OsiService.Id,
                    GroupId = t.OsiService.ServiceGroupId,
                    ServiceName = t.OsiService.NameRu,
                    Tarif = t.OsiServiceAmount.Amount,
                    Debet = (t.TransactionType.In(TransactionTypeCodes.ACC, TransactionTypeCodes.FIX) && t.Amount >= 0) ? Math.Round(t.Amount, 2) : 0,
                    DebetWithoutFixes = t.TransactionType == TransactionTypeCodes.ACC ? Math.Round(t.Amount, 2) : 0,
                    SumOfFixes = (t.TransactionType == TransactionTypeCodes.FIX/* && t.Amount >= 0*/) ? Math.Round(t.Amount, 2) : 0 // 14-03-2024, переписка в слаке
                }).ToListAsync();

            return accurals;
        }
    }
}
