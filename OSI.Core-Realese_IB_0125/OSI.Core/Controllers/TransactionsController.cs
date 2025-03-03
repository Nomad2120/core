using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Reports;
using OSI.Core.Models.Reports.SaldoOnAllPeriod;
using OSI.Core.Models.Requests;
using OSI.Core.Models.Responses;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Движения по счету абонента (проводки/транзакции)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionSvc transactionSvc;
        private readonly ILogger<TransactionsController> logger;

        public TransactionsController(ITransactionSvc transactionSvc, ILogger<TransactionsController> logger)
        {
            this.transactionSvc = transactionSvc;
            this.logger = logger;
        }

        /// <summary>
        /// Получить сальдо по абоненту
        /// </summary>
        /// <param name="abonentId">Id абонента</param>
        /// <returns></returns>
        [HttpGet("saldo/{abonentId:int}")]
        [Authorize(Roles.All)]
        [UserHasAccessFilter(typeof(Abonent), nameof(abonentId), Roles = Roles.Support | Roles.PAYMENTSERVICE)]
        public async Task<ApiResponse<EndSaldoResponse>> GetEndSaldoOnDateByAbonentId(int abonentId)
        {
            var apiResponse = new ApiResponse<EndSaldoResponse>();
            try
            {
                var endSaldo = await transactionSvc.GetActiveEndSaldoOnDateByAbonentId(DateTime.Now, abonentId);
                apiResponse.Result = endSaldo;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 100);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получить сальдо по абоненту за период
        /// </summary>
        /// <param name="abonentId">Id абонента</param>
        /// <param name="dateBegin">Начало периода</param>
        /// <param name="dateEnd">Конец периода</param>
        /// <returns></returns>
        [HttpGet("saldo-on-period/{abonentId:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(Abonent), nameof(abonentId))]
        public async Task<ApiResponse<OSVAbonent>> GetEndSaldoOnPeriodByAbonentId(int abonentId, [FromQuery][Required] DateTime dateBegin, [FromQuery][Required] DateTime dateEnd)
        {
            var apiResponse = new ApiResponse<OSVAbonent>();
            try
            {
                var endSaldo = await transactionSvc.GetEndSaldoOnPeriodByAbonentId(dateBegin, dateEnd, abonentId);
                apiResponse.Result = endSaldo;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 100);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получить сальдо по абоненту за все периоды
        /// </summary>
        /// <param name="abonentId">Id абонента</param>
        /// <returns></returns>
        [HttpGet("saldo-on-all-periods/{abonentId:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(Abonent), nameof(abonentId))]
        public async Task<ApiResponse<IEnumerable<SaldoPeriod>>> GetEndSaldoOnAllPeriodByAbonentId(int abonentId)
        {
            var apiResponse = new ApiResponse<IEnumerable<SaldoPeriod>>();
            try
            {
                var endSaldo = await transactionSvc.GetEndSaldoOnAllPeriodByAbonentId(abonentId);
                apiResponse.Result = endSaldo;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 100);
            }
            return apiResponse;
        }

        /// <summary>
        /// Оборотно-сальдовая ведомость по всем абонентам и услугам ОСИ за период
        /// </summary>
        /// <param name="osiId">Id ОСИ</param>
        /// <param name="dateBegin">Начало периода</param>
        /// <param name="dateEnd">Конец периода</param>
        /// <returns></returns>
        [HttpGet("osv/{osiId:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(Osi), nameof(osiId))]
        public async Task<ApiResponse<OSV>> GetOSVOnDateByOsiId(int osiId, [FromQuery][Required] DateTime dateBegin, [FromQuery][Required] DateTime dateEnd)
        {
            var apiResponse = new ApiResponse<OSV>();
            try
            {
                var osv = await transactionSvc.GetOSVOnDateByOsiId(dateBegin, dateEnd, osiId);
                apiResponse.Result = osv;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 100);
            }
            return apiResponse;
        }

        /// <summary>
        /// Оборотно-сальдовая ведомость по всем абонентам и услугам ОСИ за период для отчета "Должники"
        /// </summary>
        /// <param name="osiId">Id ОСИ</param>
        /// <param name="dateBegin">Начало периода</param>
        /// <param name="dateEnd">Конец периода</param>
        /// <returns></returns>
        [HttpGet("osv-for-debtors/{osiId:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(Osi), nameof(osiId))]
        public async Task<ApiResponse<OSV>> GetOSVForDebtorsOnDateByOsiId(int osiId, [FromQuery][Required] DateTime dateBegin, [FromQuery][Required] DateTime dateEnd)
        {
            var apiResponse = new ApiResponse<OSV>();
            try
            {
                var osv = await transactionSvc.GetOSVOnDateByOsiId(dateBegin, dateEnd, osiId, forDebtors: true);
                apiResponse.Result = osv;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 100);
            }
            return apiResponse;
        }

        /// <summary>
        /// Оборотно-сальдовая ведомость по всем абонентам и услугам ОСИ за период с 1 по последнее число текущего месяца
        /// </summary>
        /// <param name="osiId">Id ОСИ</param>
        /// <returns></returns>
        [HttpGet("osv-current-month/{osiId:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(Osi), nameof(osiId))]
        public async Task<ApiResponse<OSV>> GetOSVOnDateByOsiIdCurrent(int osiId)
        {
            var apiResponse = new ApiResponse<OSV>();
            try
            {
                DateTime today = DateTime.Today;
                DateTime dateBegin = new DateTime(today.Year, today.Month, 1);
                DateTime dateEnd = dateBegin.AddMonths(1);
                var osv = await transactionSvc.GetOSVOnDateByOsiId(dateBegin, dateEnd, osiId);
                apiResponse.Result = osv;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 100);
            }
            return apiResponse;
        }

        /// <summary>
        /// Платежи по всем абонентам и услугам ОСИ за период
        /// </summary>
        /// <param name="osiId">Id ОСИ</param>
        /// <param name="dateBegin">Начало периода</param>
        /// <param name="dateEnd">Конец периода</param>
        /// <returns></returns>
        [HttpGet("payments/{osiId:int}")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(osiId))]
        public async Task<ApiResponse<List<PaymentTransaction>>> GetPaymentsOnDateByOsiId(int osiId, [FromQuery][Required] DateTime dateBegin, [FromQuery][Required] DateTime dateEnd)
        {
            var apiResponse = new ApiResponse<List<PaymentTransaction>>();
            try
            {
                var payments = await transactionSvc.GetPaymentsOnDateByOsiId(dateBegin, dateEnd, osiId);
                apiResponse.Result = payments;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 100);
            }
            return apiResponse;
        }

        /// <summary>
        /// Корректировки по всем абонентам и услугам ОСИ за период
        /// </summary>
        /// <param name="osiId">Id ОСИ</param>
        /// <param name="dateBegin">Начало периода</param>
        /// <param name="dateEnd">Конец периода</param>
        /// <returns></returns>
        [HttpGet("fixes/{osiId:int}")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(osiId))]
        public async Task<ApiResponse<List<FixTransaction>>> GetFixesOnDateByOsiId(int osiId, [FromQuery][Required] DateTime dateBegin, [FromQuery][Required] DateTime dateEnd)
        {
            var apiResponse = new ApiResponse<List<FixTransaction>>();
            try
            {
                var fixes = await transactionSvc.GetFixesOnDateByOsiId(dateBegin, dateEnd, osiId);
                apiResponse.Result = fixes;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 100);
            }
            return apiResponse;
        }

        /// <summary>
        /// Создать платеж
        /// </summary>
        /// <param name="request">Запрос на создание платежа</param>
        /// <param name="bankCode">Код банка</param>
        /// <returns>Id и референс платежа</returns>
        [HttpPost("payment")]
        [Authorize(Roles.PAYMENTSERVICE)]
        public async Task<ApiResponse<CreatePaymentResponse>> CreatePayment(
            [FromBody][Required] CreatePaymentRequest request,
            [FromQuery][Required] string bankCode)
        {
            var apiResponse = new ApiResponse<CreatePaymentResponse>();
            try
            {
#if !DEBUG
                if (bankCode == "OSI" /*|| bankCode == "4254"*/)
                {
                    apiResponse.FromError("Неверный код банка", 301);
                }
#endif
                if (apiResponse.Code == 0)
                {
                    apiResponse = await transactionSvc.CreatePayment(bankCode, null, request);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 200);
            }
            return apiResponse;
        }

        /// <summary>
        /// Создать платеж
        /// </summary>
        /// <param name="request">Запрос на создание платежа</param>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Id платежа</returns>
        [HttpPost("payment/internal")]
        [Authorize(Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(User), nameof(userId))]
        [UserHasAccessFilter(typeof(Abonent), nameof(request), nameof(CreatePaymentInternalRequest.AbonentNum))]
        public async Task<ApiResponse<int>> CreatePaymentInternal(
            [FromBody][Required] CreatePaymentInternalRequest request,
            [FromQuery][Required] int userId)
        {
            var apiResponse = new ApiResponse<int>();
            try
            {
                apiResponse = await transactionSvc.CreatePaymentInternal(userId, request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Создать корректировку
        /// </summary>
        /// <param name="request">Запрос на создание корректировки</param>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Id корректировки</returns>
        [HttpPost("fix/internal")]
        [Authorize(Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(User), nameof(userId))]
        [UserHasAccessFilter(typeof(Abonent), nameof(request), nameof(CreatePaymentInternalRequest.AbonentNum))]
        public async Task<ApiResponse<int>> CreateFixInternal(
            [FromBody][Required] CreateFixRequest request,
            [FromQuery][Required] int userId)
        {
            var apiResponse = new ApiResponse<int>();
            try
            {
                apiResponse = await transactionSvc.CreateFix(userId, request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }


        /// <summary>
        /// Начислить пеню
        /// </summary>
        /// <param name="osiId">Id ОСИ</param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц</param>
        /// <returns></returns>
        [HttpPost("fine")]
        [Authorize(Roles.ADMIN)]
        [UserHasAccessFilter(typeof(Osi), nameof(osiId))]
        public Task<ApiResponse> CreateFine([FromQuery][Required] int osiId, [FromQuery][Required] int year, [FromQuery][Required] int month) =>
            ApiResponse.CreateEx(() => transactionSvc.CreateFine(osiId, year, month));

        /// <summary>
        /// Отчет по начислениям абонента за период
        /// </summary>
        /// <param name="abonentId">ID абонента</param>
        /// <param name="dateBegin">Начало периода</param>
        /// <param name="dateEnd">Конец периода</param>
        /// <returns></returns>
        [HttpGet("accurals-by-abonent-and-services/{abonentId:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(Abonent), nameof(abonentId))]
        public async Task<ApiResponse<List<Models.Reports.AccuralsByAbonentAndServices.Group>>> AccuralsByAbonentAndServices(int abonentId, [FromQuery][Required] DateTime dateBegin, [FromQuery][Required] DateTime dateEnd)
        {
            var apiResponse = new ApiResponse<List<Models.Reports.AccuralsByAbonentAndServices.Group>>();
            try
            {
                var report = await transactionSvc.AccuralsByAbonentAndServices(abonentId, dateBegin, dateEnd);
                apiResponse.Result = report;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 100);
            }
            return apiResponse;
        }

        /*/// Получение Id пользователя по токену
        /// <summary>
        /// Создать платеж
        /// </summary>
        /// <param name="request">Запрос на создание платежа</param>
        /// <returns>Id платежа</returns>
        [HttpPost("payment/internal")]
        [Authorize(Roles = "CHAIRMAN")]
        public async Task<ApiResponse<CreatePaymentResponse>> CreatePaymentInternal(
            [FromBody][Required] CreatePaymentRequest request)
        {
            var apiResponse = new ApiResponse<CreatePaymentResponse>();
            try
            {
                string userData = User.FindFirstValue(ClaimTypes.UserData);
                int userId = Convert.ToInt32(userData);
                var createPaymentResponse = await transactionSvc.CreatePayment("OSI", userId, request);
                apiResponse = createPaymentResponse.ToApiResponse(new CreatePaymentResponse
                {
                    PaymentId = createPaymentResponse.Result,
                    Reference = request.Reference,
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 200);
            }
            return apiResponse;
        }

        /// <summary>
        /// Создать корректировку
        /// </summary>
        /// <param name="request">Запрос на создание корректировки</param>
        /// <returns>Id корректировки</returns>
        [HttpPost("fix/internal")]
        [Authorize(Roles = "CHAIRMAN")]
        public async Task<ApiResponse<CreateFixResponse>> CreateFixInternal(
            [FromBody][Required] CreateFixRequest request)
        {
            var apiResponse = new ApiResponse<CreateFixResponse>();
            try
            {
                string userData = User.FindFirstValue(ClaimTypes.UserData);
                int userId = Convert.ToInt32(userData);
                var createFixResponse = await transactionSvc.CreateFix(userId, request);
                apiResponse = createFixResponse.ToApiResponse(new CreateFixResponse
                {
                    FixId = createFixResponse.Result,
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 200);
            }
            return apiResponse;
        }
        /**/
    }
}
