using OSI.Core.DataAnnotations;
using OSI.Core.Models.Responses;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class CreatePaymentInternalRequest
    {
        /// <summary>
        /// Код абонента
        /// </summary>
        [RequiredExt]
        public int AbonentNum { get; set; }

        /// <summary>
        /// Дата и время оплаты
        /// </summary>
        [RequiredExt]
        public DateTime Date { get; set; }

        /// <summary>
        /// Список услуг
        /// </summary>
        [RequiredExt]
        public List<PaymentInternalService> Services { get; set; }
    }
}
