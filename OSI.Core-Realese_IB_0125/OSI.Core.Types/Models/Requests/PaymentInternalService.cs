using OSI.Core.Models.Responses;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class PaymentInternalService
    {
        /// <summary>
        /// Код услуги
        /// </summary>
        [Required]
        public int ServiceGroupId { get; set; }

        /// <summary>
        /// Сумма к оплате
        /// </summary>
        [Required]
        public decimal Sum { get; set; }
    }
}
