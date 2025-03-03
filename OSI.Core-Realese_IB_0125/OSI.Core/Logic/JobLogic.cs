using OSI.Core.Helpers;
using OSI.Core.Models;
using OSI.Core.Models.Jobs;
using OSI.Core.Services;
using System;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IJobLogic
    {
        Task<ApiResponse> SendDBFToKazPost(JobParameters parameters);
    }

    /// <summary>
    /// Запуск процедур джобов с параметрами
    /// </summary>
    public class JobLogic : IJobLogic
    {
        private readonly ISendEmailSvc sendEmailSvc;
        private readonly ITelegramNotificationSvc telegramNotificationSvc;
        private readonly IReportsSvc reportsSvc;
        private readonly IPaymentOrderSvc paymentOrderSvc;

        public JobLogic(ISendEmailSvc sendEmailSvc, ITelegramNotificationSvc telegramNotificationSvc, IReportsSvc reportsSvc, IPaymentOrderSvc paymentOrderSvc)
        {
            this.sendEmailSvc = sendEmailSvc;
            this.telegramNotificationSvc = telegramNotificationSvc;
            this.reportsSvc = reportsSvc;
            this.paymentOrderSvc = paymentOrderSvc;
        }

        /// <summary>
        /// Отправка Dbf Казпочте
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<ApiResponse> SendDBFToKazPost(JobParameters parameters)
        {
            var result = new ApiResponse();

            string period = parameters.StartDate.ToString("dd.MM.yyyy");
            string subject = $"Ведомость распределения платежей за {period}";
            var apiResponse = new ApiResponse();
            try
            {
                var processPayment = await paymentOrderSvc.ProcessPayments("KPST", parameters.StartDate);
                // игнорируем 502 ошибку, когда не будет ни одного платежа CREATED, чтобы выполнить след.метод - PaymentOrders
                if (processPayment.Code != 0 && processPayment.Code != 502)
                {
                    throw new Exception(processPayment.Message);
                }

                // здесь 502 ошибка выпадет в эксепшен, так и надо
                var getDbf = await reportsSvc.GetPaymentOrdersDBFKazPost(parameters.StartDate);
                if (getDbf.Code != 0)
                {
                    throw new Exception(getDbf.Message);
                }

                string message = "Данное письмо сгенерировано сервером, на него отвечать не надо. Если возникнут вопросы, обращатесь к администратору: ussiksamara@gmail.com";

                var send = await sendEmailSvc.SendEmail(parameters.Email, subject, message, attachmentName: getDbf.Result);
                if (send.Code == 0)
                {
                    apiResponse.Message = "Выполнено";
                    //await telegramNotificationSvc.SendNotificationAsync("armgai", subject, apiResponse.Message + ". Найдено платежей: " + list.Count, silent: true);
                }
                else throw new Exception(send.Message);
            }
            catch (Exception ex)
            {
                apiResponse.Code = -1;
                apiResponse.Message = ex.GetFullInfo(includeInnerExceptions: true);
                await telegramNotificationSvc.SendNotificationAsync("osi_errors", "Казпочта: " + subject, apiResponse.Message, silent: false);
            }

            return apiResponse;
        }
    }
}
