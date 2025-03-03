using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class PlanAccuralServiceRequest
    {
        /// <summary>
        /// Услуга ОСИ
        /// </summary>
        [Required(ErrorMessage = "Укажите услугу ОСИ")]
        public int OsiServiceId { get; set; }

        /// <summary>
        /// План начислений
        /// </summary>
        [Required(ErrorMessage = "Укажите план начислений")]
        public int PlanAccuralId { get; set; }

        /// <summary>
        /// Величина тариф или общей суммы, взависимости от типа
        /// </summary>
        [Required(ErrorMessage = "Укажите сумму")]
        public decimal Amount { get; set; }
    }
}
