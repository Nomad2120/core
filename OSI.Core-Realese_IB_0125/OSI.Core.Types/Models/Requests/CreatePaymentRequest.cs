using OSI.Core.DataAnnotations;
using OSI.Core.Models.Responses;
using OSI.Core.Swagger;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    /// <summary>
    /// Запрос на создание платежа
    /// </summary>
    public class CreatePaymentRequest
    {
        /// <summary>
        /// Номер абонента
        /// </summary>
        [RequiredExt]
        public string AbonentNum { get; set; }

        /// <summary>
        /// Дата и время оплаты
        /// </summary>
        [SwaggerIgnore]
        public DateTime Date { get; set; }

        /// <summary>
        /// Референс (уникальный идентификатор операции банка)
        /// </summary>
        [RequiredExt]
        public string Reference { get; set; }

        /// <summary>
        /// Список услуг
        /// </summary>
        [RequiredExt]
        public List<PaymentService> Services { get; set; }
    }
}
