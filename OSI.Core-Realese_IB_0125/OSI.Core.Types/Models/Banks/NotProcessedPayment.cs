using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Banks
{
    public class NotProcessedPayment
    {
        /// <summary>
        /// Id платежа
        /// </summary>
        public int PaymentId { get; set; }
        /// <summary>
        /// Номер абонента
        /// </summary>
        public string AbonentNum { get; set; }
        /// <summary>
        /// Референс (уникальный идентификатор операции банка)
        /// </summary>
        public string Reference { get; set; }
        /// <summary>
        /// Сумма платежа
        /// </summary>
        public decimal Amount { get; set; }
        /// <summary>
        /// Дата и время регистрации платежа
        /// </summary>
        public DateTime PaymentDate { get; set; }
    }
}
