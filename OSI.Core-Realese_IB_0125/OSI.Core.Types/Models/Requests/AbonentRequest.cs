using OSI.Core.Models.Enums;
using OSI.Core.Validators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class AbonentRequest
    {
        /// <summary>
        /// ОСИ
        /// </summary>
        public int OsiId { get; set; }

        /// <summary>
        /// ФИО/наименование
        /// </summary>
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Номер квартиры/помещения
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите квартиру")]
        [MaxLength(200)]
        public string Flat { get; set; }

        /// <summary>
        /// ИИН
        /// </summary>
        [IIN]
        public string Idn { get; set; }

        /// <summary>
        /// Тип помещения (жилое/нежилое)
        /// </summary>
        [Required(ErrorMessage = "Укажите тип помещения")]
        public AreaTypeCodes AreaTypeCode { get; set; }

        /// <summary>
        /// Телефон
        /// </summary>
        [RegularExpression(@"^7\d{9}$", ErrorMessage = "Укажите номер телефона в виде 7хxххххххх")]
        [MaxLength(15)]
        public string Phone { get; set; }

        /// <summary>
        /// Этаж
        /// </summary>
        [Required(ErrorMessage = "Укажите этаж")]
        public int Floor { get; set; }

        /// <summary>
        /// Площадь квартиры/помещения
        /// </summary>
        [Required(ErrorMessage = "Укажите площадь помещения")]
        public decimal Square { get; set; }

        /// <summary>
        /// Кол-во прописанных
        /// </summary>
        [Required(ErrorMessage = "Укажите кол-во прописанных человек")]
        public int LivingJur { get; set; }

        /// <summary>
        /// Кол-во живущих
        /// </summary>
        [Required(ErrorMessage = "Укажите кол-во живущих человек")]
        public int LivingFact { get; set; }

        // OSI-158 Признак владельца помещения
        /// <summary>
        /// Признак владельца: Собственник/Арендатор
        /// </summary>
        [Values("Собственник", "Арендатор")]
        public string Owner { get; set; }

        // OSI-119 Дополнительные услуги ОСИ
        /// <summary>
        /// Признак внешнего абонента (арендатора)
        /// </summary>
        public bool External { get; set; } = false;

        /// <summary>
        /// Полезная площадь квартиры/помещения
        /// </summary>
        public decimal? EffectiveSquare { get; set; }
    }
}
