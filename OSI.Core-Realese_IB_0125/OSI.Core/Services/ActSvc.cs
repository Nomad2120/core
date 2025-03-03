using ESoft.CommonLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using OSI.Core.Models.Requests;
using OSI.Core.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IActSvc
    {
        Task<Act> CreateActByPlanAccuralId(int id);
        Task<Act> CreateActByPlanAccural(PlanAccural planAccural, bool onlyIfNotExists = false);
        Task<Act> GetActById(int id);
        Task<ActResponse> GetActResponseById(int id);
        Task<ActDoc> AddActDoc(int actId, AddScanDoc request);
        Task<IEnumerable<ActDoc>> GetActsDocs(int actId);
        Task DeleteActDoc(int actId, int docId);
        Task SignActId(int id, string extension, byte[] data);
        Task SignAct(Act act, string extension, byte[] data);
        Task UnsignActId(int id);
        Task UnsignAct(Act act);
        Task<ApiResponse<EsfUploadResponse>> CreateEsf(Act act);
    }

    public class ActSvc : IActSvc
    {
        private readonly IScanSvc scanSvc;
        private readonly ITelegramBotSvc telegramBotSvc;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration;
        private readonly ILogger<ActSvc> logger;

        public ActSvc(IScanSvc scanSvc, ITelegramBotSvc telegramBotSvc, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ActSvc> logger)
        {
            this.scanSvc = scanSvc;
            this.telegramBotSvc = telegramBotSvc;
            this.httpClientFactory = httpClientFactory;
            this.configuration = configuration;
            this.logger = logger;
        }

        public string GetExceptionMessage(Exception ex)
        {
            string message = ex.Message;
            //if (ex is DbUpdateException)
            //{
            //    if (ex.InnerException.Message.IndexOf("violates foreign key constraint") > -1)
            //    {
            //        if (ex.InnerException.Message.IndexOf("fk_osi_service_saldo_abonents") > -1)
            //        {
            //            message = "По данному абоненту указано начальное сальдо по услуге. Сначала удалите сальдо";
            //        }
            //        else if (ex.InnerException.Message.IndexOf("fk_transactions_abonents") > -1)
            //        {
            //            message = "По данному абоненту уже проводились операции";
            //        }
            //        else message = "Данный абонент не может быть изменен или удален, т.к. его данные уже используются";
            //    }
            //}
            return message;
        }

        public async Task<Act> CreateActByPlanAccuralId(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var planAccural = await db.PlanAccurals.Include(p => p.Osi).FirstOrDefaultAsync(a => a.Id == id);
            if (planAccural == null)
                throw new Exception("План начислений не найден");
            return await CreateActByPlanAccural(planAccural);
        }

        public async Task<Act> CreateActByPlanAccural(PlanAccural planAccural, bool onlyIfNotExists = false)
        {
            // тут не учитывается что оси должен быть запущен и по плану должны быть начисления
            // это проверяется в выборке buhSvc, где массово создаются акты

            if (planAccural.BeginDate >= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1))
            {
                throw new Exception("Нужен план начислений, дата которого меньше текущего месяца");
            }

            using var db = OSIBillingDbContext.DbContext;

            Act act = await db.Acts.FirstOrDefaultAsync(a => a.PlanAccuralId == planAccural.Id);
            if (act != null)
            {
                if (onlyIfNotExists)
                    return act;

                if (act.StateCode == ActStateCodes.SIGNED)
                    throw new Exception("По данному плану уже есть акт и он подписан.");

                int actOperationsCount = await db.ActOperations.CountAsync(ao => ao.ActId == act.Id);
                if (actOperationsCount > 0)
                    throw new Exception("По данному акту уже были списания, переформирование невозможно");
            }

            var dopUslugiServiceGroup = await db.ServiceGroups.FirstOrDefaultAsync(a => a.Code == "ADDITIONAL");
            if (dopUslugiServiceGroup == null)
                throw new Exception("Не найдена группа \"Доп.услуги\"");

            DateTime dateBegin = planAccural.BeginDate;
            DateTime dateEnd = planAccural.BeginDate.AddMonths(1);  // последнее число месяца

            // считаем сумму начислений по ОСИ биллинг
            decimal accuralsOsi = planAccural.ApartCount * planAccural.Tariff;

            // платежи за месяц без доп услуг
            // доп.услуги никогда не будут оплачиваться вместе с другими услугам, можно смело отсекать платежи, если есть транзакция с доп.услугой
            // комиссия банка, TakeComission = true - берем ли мы комиссию на себя (компенсация)
            decimal paymentsComission1 = planAccural.Osi.TakeComission ? (await db.Payments
                .Include(p => p.Transactions)
                .Where(p => p.OsiId == planAccural.OsiId
                && p.RegistrationDate >= dateBegin.Date
                && p.RegistrationDate < dateEnd.Date
                && !p.Transactions.Any(t => t.GroupId == dopUslugiServiceGroup.Id)).SumAsync(p => p.Comission)) : 0;

            // 2% от суммы платежей за месяц по доп услугам
//            decimal paymentsComission2 = (await db.Payments
//                .Include(p => p.Transactions)
//                .Include(p => p.Contract)
//                .Where(p => p.OsiId == planAccural.OsiId
//                && p.RegistrationDate >= dateBegin.Date
//                && p.RegistrationDate < dateEnd.Date
//                && p.Transactions.Any(t => t.GroupId == dopUslugiServiceGroup.Id)
//#if !DEBUG
//                    && p.Contract.BankCode != "OSI"
//#endif
//                )
//                .SumAsync(p => p.Amount)) * 0.02m;

            var osi = planAccural.Osi;

            using var dbTransaction = await db.Database.BeginTransactionAsync();
            try
            {
                if (act != null)
                {
                    // ищем списание месяцев по акции
                    var promoOperation = await db.PromoOperations.FirstOrDefaultAsync(p => p.ActId == act.Id);
                    if (promoOperation != null)
                    {
                        db.PromoOperations.Remove(promoOperation);

                        // увеличиваем кол-во доступных месяцев
                        osi.FreeMonthPromo++;
                        db.Entry(osi).Property(a => a.FreeMonthPromo).IsModified = true;
                    }
                    db.Acts.Remove(act);
                }

                DateTime actCreateDate = DateTime.Now;
                act = new Act
                {
                    ActNum = planAccural.Id.ToString().PadLeft(10, '0'),
                    ActPeriod = dateEnd.AddDays(-1),
                    CreateDt = actCreateDate,
                    OsiId = planAccural.OsiId,
                    StateCode = ActStateCodes.CREATED,
                    PlanAccuralId = planAccural.Id
                };

                // первая строка
                act.ActItems.Add(new ActItem
                {
                    Description = "Ежемесячная оплата облачной биллинговой платформы \"eosi.kz\" за " + act.ActDateStr,
                    Amount = accuralsOsi,
                    Quantity = planAccural.ApartCount,
                    Price = planAccural.Tariff
                });

                // OSI-311,312 проверяем оставшиеся месяцы и делаем скидку
                bool addPromoOperation = false;
                if (osi.FreeMonthPromo > 0)
                {
                    // уменьшаем месяцы и делаем скидку 100%
                    osi.FreeMonthPromo--;
                    db.Entry(osi).Property(a => a.FreeMonthPromo).IsModified = true;
                    act.Debt = 0;
                    act.ActItems.Add(new ActItem
                    {
                        Description = "Скидка 100%",
                        Amount = -accuralsOsi
                    });
                    addPromoOperation = true;
                }
                else
                {
                    // нет месяцев - нет скидки
                    act.Debt = accuralsOsi - paymentsComission1;
                }

                //if (paymentsComission2 > 0)
                //{
                //    act.ActItems.Add(new ActItem
                //    {
                //        Description = "За дополнительные услуги системы \"ОСИ биллинг\" за " + act.ActDateStr,
                //        Amount = paymentsComission2
                //    });
                //    act.Debt += paymentsComission2;
                //}

                act.Amount = accuralsOsi;// + paymentsComission2;
                act.Comission = paymentsComission1;
                act.Tariff = planAccural.Tariff;
                if (addPromoOperation)
                {
                    act.PromoOperations.Add(new PromoOperation
                    {
                        ActId = act.Id,
                        OsiId = osi.Id,
                        Dt = actCreateDate
                    });
                }
                db.Acts.Add(act);

                await db.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }

#if !DEBUG
            _ = telegramBotSvc.SendActCreatedNotification(act.Id);
#endif
            return act;
        }

        public async Task<Act> GetActById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var act = await db.Acts
                .Include(a => a.ActItems)
                .Include(a => a.State)
                .Include(a => a.Osi)
                .ThenInclude(o => o.Registration)
                .Include(a => a.PlanAccural)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (act == null)
                throw new Exception("Акт не найден");

            return act;
        }

        public async Task<ActResponse> GetActResponseById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var act = await db.Acts
                .Include(a => a.ActItems)
                .Include(a => a.State)
                .Include(a => a.Osi)
                .ThenInclude(o => o.Registration)
                .Include(a => a.PlanAccural).FirstOrDefaultAsync(a => a.Id == id);

            if (act == null)
                throw new Exception("Акт не найден");

            var reponse = new ActResponse
            {
                Id = act.Id,
                ActNum = act.ActNum,
                ActPeriod = act.ActPeriod,
                Amount = act.Amount,
                Comission = act.Comission,
                Tariff = act.Tariff,
                CreateDt = act.CreateDt,
                Debt = act.Debt,
                Osi = act.Osi,
                PlanAccuralId = act.PlanAccuralId,
                SignDt = act.SignDt,
                StateCode = act.StateCode,
                ActItems = act.ActItems.ToList()
            };

            return reponse;
        }

        public async Task<ActDoc> AddActDoc(int actId, AddScanDoc request)
        {
            using var db = OSIBillingDbContext.DbContext;
            Act act = await GetActById(actId);

            string fileName = "act_" + act.ActNum + "_" + act.ActPeriod.ToString("dd-MM-yyyy") + "." + request.Extension.Replace(".", "");
            Scan scan = await scanSvc.SaveDataToFile(fileName, request.Data);

            ActDoc doc = new ActDoc
            {
                DocTypeCode = request.DocTypeCode,
                ActId = actId,
                ScanId = scan.Id
            };
            db.ActDocs.Add(doc);
            await db.SaveChangesAsync();

            doc.Scan = scan;
            doc.DocType = await db.DocTypes.FirstOrDefaultAsync(d => d.Code == doc.DocTypeCode);
            return doc;
        }

        public async Task<IEnumerable<ActDoc>> GetActsDocs(int actId)
        {
            using var db = OSIBillingDbContext.DbContext;
            var actDocs = await db.ActDocs
                .Include(a => a.DocType)
                .Include(a => a.Scan)
                .Where(a => a.ActId == actId).ToListAsync();
            return actDocs;
        }

        public async Task DeleteActDoc(int actId, int docId)
        {
            using var db = OSIBillingDbContext.DbContext;
            ActDoc doc = await db.ActDocs.FirstOrDefaultAsync(ad => ad.Id == docId && ad.ActId == actId);
            if (doc == null)
                throw new Exception("Документ не найден");

            db.ActDocs.Remove(doc);

            // если нет такого скана, то удаляем из сканов
            if (!db.OsiDocs.Any(r => r.ScanId == doc.ScanId))
            {
                await scanSvc.DeleteScanById(doc.ScanId);
            }

            await db.SaveChangesAsync();
        }

        public async Task SignActId(int id, string extension, byte[] data)
        {
            using var db = OSIBillingDbContext.DbContext;
            var act = await GetActById(id);
            await SignAct(act, extension, data);
        }

        public async Task SignAct(Act act, string extension, byte[] data)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (act.StateCode != ActStateCodes.CREATED)
            {
                var mustBeState = await db.ActStates.FirstOrDefaultAsync(a => a.Code == ActStateCodes.CREATED);
                throw new Exception($"Акт должен быть в состоянии '{mustBeState?.Name}'");
            }

            var signDoc = await AddActDoc(act.Id, new AddScanDoc
            {
                DocTypeCode = "SIGNED_ACT",
                Data = data,
                Extension = extension
            });

            act.StateCode = ActStateCodes.SIGNED;
            act.State = null;
            act.SignDt = DateTime.Now;

            db.Acts.Update(act);
            await db.SaveChangesAsync();
        }

        public async Task UnsignActId(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var act = await GetActById(id);
            await UnsignAct(act);
        }

        public async Task UnsignAct(Act act)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (act.StateCode != ActStateCodes.SIGNED)
            {
                var mustBeState = await db.ActStates.FirstOrDefaultAsync(a => a.Code == ActStateCodes.SIGNED);
                throw new Exception($"Акт должен быть в состоянии '{mustBeState?.Name}'");
            }

            // если уже есть подписанный акт, то удалим старый
            var actDocs = await GetActsDocs(act.Id);
            if (actDocs != null && actDocs.Any(ad => ad.DocTypeCode == "SIGNED_ACT"))
            {
                var oldSignDoc = actDocs.FirstOrDefault(ad => ad.DocTypeCode == "SIGNED_ACT");
                await DeleteActDoc(act.Id, oldSignDoc.Id);
            }

            act.StateCode = ActStateCodes.CREATED;
            act.State = null;
            act.SignDt = null;

            db.Acts.Update(act);
            await db.SaveChangesAsync();
        }

        public async Task<ApiResponse<EsfUploadResponse>> CreateEsf(Act act)
        {
            using var db = OSIBillingDbContext.DbContext;
            var apiResponse = new ApiResponse<EsfUploadResponse>();

            // 14-05-2024, shuma ------------------------------
            // доработка, чтобы не создавались СФ у которых сумма удержания 0
            // алгоритм скопирован с отчета актам
            decimal actSkidka = 0;
            //decimal dopUslugiSumma = await db.ActItems
            //    .Where(a => a.ActId == act.Id)
            //    .Where(a => a.Description.StartsWith("За дополнительные услуги"))
            //    .SumAsync(a => a.Amount);

            if (act.ActItems.Any(a => a.Description.StartsWith("Скидка")))
               actSkidka = act.ActItems
                    .Where(a => a.Description.StartsWith("Скидка"))
                    .Sum(a => a?.Amount ?? 0);
                        
            decimal summaUderzhaniya = (act.Osi.TakeComission ? ((act?.Amount - act?.Comission) ?? 0) : act?.Amount) ?? 0; // сумма удержания                 
            summaUderzhaniya -= Math.Abs(actSkidka); // скидка идет с минусом, поэтому чтобы не запутаться делаем Math.Abs
            if (summaUderzhaniya <= 0) 
            {
                string msg = $"Акт Id {act.Id}: Сумма удержания = {summaUderzhaniya}, СФ не будет создана";
                logger.LogInformation(msg);
                throw new Exception(msg);
            }
            //----------------------------------------------

            logger.LogInformation($"Акт Id {act.Id}: Создание ЭСФ");
            try
            {
                //if (act.StateCode != ActStateCodes.SIGNED)
                //    throw new Exception($"Акт должен быть подписан");

                if (!string.IsNullOrEmpty(act.EsfNum))
                    throw new Exception($"Счет-фактура уже создана");

                var contract = await db.OsiDocs.OrderByDescending(o => o.CreateDt).FirstOrDefaultAsync(o => o.OsiId == act.Osi.Id && o.DocTypeCode == "SIGNED_CONTRACT");
                if (contract == null) throw new Exception("Отсутствует подписанный договор");

                var osi = act.Osi;
                var plan = act.PlanAccural;
                var descriptionDate = act.ActPeriod.Month switch
                {
                    1 => "январь",
                    2 => "февраль",
                    3 => "март",
                    4 => "апрель",
                    5 => "май",
                    6 => "июнь",
                    7 => "июль",
                    8 => "август",
                    9 => "сентябрь",
                    10 => "октябрь",
                    11 => "ноябрь",
                    12 => "декабрь",
                    _ => ""
                } + " " + act.ActPeriod.Year + " г.";

                EsfCreateRequest request = new()
                {
                    ActDate = act.ActPeriod,
                    ActNumber = act.ActNum,
                    Amount = act.Amount,
                    ContractDate = contract.CreateDt.Value,
                    CustomerAddress = osi.Address,
                    CustomerBin = osi.Idn,
                    CustomerName = osi.Name,
                    Num = act.Id.ToString(),
                    OperatorFullName = "Кравцова Марина Сергеевна",
                    ProductDescription = "Ежемесячная оплата облачной биллинговой платформы \"eosi.kz\" за " + descriptionDate,
                    ProductPrice = act.Tariff,
                    ProductQuantity = act.PlanAccural.ApartCount,
                    TurnoverDate = act.ActPeriod
                };

                using HttpClient client = httpClientFactory.CreateClient();
#if DEBUG
                //string requestUri = "http://localhost:5127/api/invoices/send";
                string requestUri = "http://10.1.1.125:8010/api/invoices/send";
#else
                string requestUri = configuration["Urls:EsfClientUrl"].Trim('/') + $"/invoices/send";
#endif
                act.EsfError = "";

                var content = JsonConvert.SerializeObject(request);
                logger.LogInformation($"Request: {requestUri} {JsonConvert.SerializeObject(content)}");
                var httpResponseMessage = await client.PostAsync(requestUri, new StringContent(content, Encoding.UTF8, "application/json"));
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    var response = await httpResponseMessage.Content.ReadFromJsonAsync<ApiResponse<EsfUploadResponse>>();
                    if (response.Code == 0)
                    {
                        if (response.Result.IsSuccess)
                        {
                            act.EsfNum = response.Result.Id.ToString();
                            db.Entry(act).Property(a => a.EsfNum).IsModified = true;
                            logger.LogInformation("Success: " + JsonConvert.SerializeObject(response));
                            apiResponse.Result = response.Result;
                        }
                        else throw new Exception(response.Result.ErrorMessage);
                    }
                    else throw new Exception(response.Message);
                }
                else throw new Exception((int)httpResponseMessage.StatusCode + ": " + httpResponseMessage.ReasonPhrase);            
            }
            catch (Exception ex)
            {
                string message = ex.InnerException?.Message ?? ex.Message;
                act.EsfError = message.Length > 200 ? message[..200] : message;
                apiResponse.Code = -1;
                apiResponse.Message = message;
                logger.LogInformation("Error: " + message);
                logger.LogError("Error: " + message);
            }

            db.Entry(act).Property(a => a.EsfError).IsModified = true;
            await db.SaveChangesAsync();
            return apiResponse;
        }
    }
}
