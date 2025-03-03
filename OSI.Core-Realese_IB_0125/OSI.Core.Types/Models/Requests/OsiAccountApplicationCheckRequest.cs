using IbanNet.DataAnnotations;
using OSI.Core.Models.Enums;
using OSI.Core.Validators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class OsiAccountApplicationCheckRequest
    {
        [Values("ADD", "UPDATE", /*"DELETE",*/ ErrorMessage = "Указан неверный тип заявки")]
        public string ApplicationType { get; set; }

        /// <summary>
        /// ОСИ
        /// </summary>
        [Required(ErrorMessage = "Укажите ОСИ")]
        public int OsiId { get; set; }

        /// <summary>
        /// Счет ОСИ
        /// </summary>
        public int? OsiAccountId { get; set; }

        /// <summary>
        /// Укажите тип счета
        /// </summary>
        [JsonPropertyName("type")]
        [Required(ErrorMessage = "Укажите тип счета")]
        public AccountTypeCodes AccountTypeCode { get; set; }

        /// <summary>
        /// Группа услуг
        /// </summary>
        public int? ServiceGroupId { get; set; }
    }
}
