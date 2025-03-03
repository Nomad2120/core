using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Banks;
using OSI.Core.Models.Reports;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Платежные поручения
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentOrdersController : ControllerBase
    {
        private readonly IPaymentOrderSvc paymentOrderSvc;
        private readonly ILogger<PaymentOrdersController> logger;

        public PaymentOrdersController(IPaymentOrderSvc paymentOrderSvc, ILogger<PaymentOrdersController> logger)
        {
            this.paymentOrderSvc = paymentOrderSvc;
            this.logger = logger;
        }

        /// <summary>
        /// Свод платежных поручений по ОСИ за период
        /// </summary>
        /// <param name="osiId">Id ОСИ</param>
        /// <param name="dateBegin">Начало периода</param>
        /// <param name="dateEnd">Конец периода</param>
        /// <returns></returns>
        [HttpGet("svod/{osiId:int}")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Models.Db.Osi), nameof(osiId))]
        public async Task<ApiResponse<IEnumerable<SvodPaymentOrder>>> GetSvodPaymentOrdersByOsiId(int osiId, [FromQuery][Required] DateTime dateBegin, [FromQuery][Required] DateTime dateEnd)
        {
            var apiResponse = new ApiResponse<IEnumerable<SvodPaymentOrder>>();
            try
            {
                var list = await paymentOrderSvc.GetSvodPaymentOrdersByOsiId(osiId, dateBegin, dateEnd);
                apiResponse.Result = list.Result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 100);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получение информации о необработанных платежах за указанную дату
        /// </summary>
        /// <param name="bankCode">Код банка</param>
        /// <param name="date">Дата</param>
        /// <returns>Список необработанных платежей</returns>
        [HttpGet("NotProcessedPayments")]
        [Authorize(Roles.PAYMENTSERVICE)]
        public async Task<ApiResponse<IEnumerable<NotProcessedPayment>>> GetNotProcessedPayments(
            [FromQuery][Required] string bankCode,
            [FromQuery][Required] DateTime date)
        {
            var apiResponse = new ApiResponse<IEnumerable<NotProcessedPayment>>();
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
                    apiResponse = await paymentOrderSvc.GetNotProcessedPayments(bankCode, date);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 500);
            }
            return apiResponse;
        }

        /// <summary>
        /// Передача Банком статуса успешной сверки платежей
        /// </summary>
        /// <param name="bankCode">Код банка</param>
        /// <param name="date">Дата</param>
        /// <returns>Id и референс платежа</returns>
        [HttpPost("ProcessPayments")]
        [Authorize(Roles.PAYMENTSERVICE)]
        public async Task<ApiResponse> ProcessPayments(
            [FromQuery][Required] string bankCode,
            [FromQuery][Required] DateTime date)
        {
            var apiResponse = new ApiResponse();
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
                    apiResponse = await paymentOrderSvc.ProcessPayments(bankCode, date);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 500);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получение информации о необработанных платежах за указанную дату
        /// </summary>
        /// <param name="bankCode">Код банка</param>
        /// <param name="date">Дата</param>
        /// <returns>Список необработанных платежей</returns>
        [HttpGet("PaymentOrders")]
        [Authorize(Roles.PAYMENTSERVICE)]
        public async Task<ApiResponse<IEnumerable<PaymentOrder>>> GetPaymentOrders(
            [FromQuery][Required] string bankCode,
            [FromQuery][Required] DateTime date)
        {
            var apiResponse = new ApiResponse<IEnumerable<PaymentOrder>>();
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
                    apiResponse = await paymentOrderSvc.GetPaymentOrders(bankCode, date);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 500);
            }
            return apiResponse;
        }
    }
}
