using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    /// <summary>
    /// Ответ на создание платежа
    /// </summary>
    public class CreatePaymentResponse
    {
        /// <summary>
        /// Id платежа
        /// </summary>
        public int PaymentId { get; set; }

        /// <summary>
        /// Дата и время регистрации платежа
        /// </summary>
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        /// Референс (уникальный идентификатор операции банка)
        /// </summary>
        public string Reference { get; set; }
    }
}
