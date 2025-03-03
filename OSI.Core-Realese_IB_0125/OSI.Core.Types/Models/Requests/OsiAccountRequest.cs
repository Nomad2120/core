using IbanNet.DataAnnotations;
using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class OsiAccountRequest
    {
        /// <summary>
        /// Объект ОСИ
        /// </summary>
        [Required(ErrorMessage = "Укажите ОСИ")]
        public int OsiId { get; set; }

        /// <summary>
        /// Укажите тип счета
        /// </summary>
        [JsonPropertyName("type")]
        [Required(ErrorMessage = "Укажите тип счета")]
        public AccountTypeCodes AccountTypeCode { get; set; }

        /// <summary>
        /// БИК банка
        /// </summary>
        [Column("bic")]
        [JsonPropertyName("bic")]
        [Required(ErrorMessage = "Укажите БИК банка")]
        public string BankBic { get; set; }

        /// <summary>
        /// Cчет
        /// </summary>
        [Required(ErrorMessage = "Укажите счет")]
        [Iban(ErrorMessage = "Указан неверный счет", Strict = true)]
        [MaxLength(20)]
        public string Account { get; set; }

        /// <summary>
        /// Группа услуг
        /// </summary>
        public int? ServiceGroupId { get; set; }
    }
}
