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
    public class OsiRequest
    {
        /// <summary>
        /// Наименование ОСИ
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите наименование ОСИ")]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// ИИН / БИН
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите ИИН / БИН")]
        [MaxLength(12)]
        [IIN]
        public string Idn { get; set; }

        /// <summary>
        /// Адрес из заявки
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите адрес")]
        [MaxLength(100)]
        public string Address { get; set; }

        /// <summary>
        /// Телефон
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите номер телефона в виде 7ххххххххх")]
        [RegularExpression(@"^7\d{9}$", ErrorMessage = "Укажите номер телефона в виде 7хxххххххх")]
        [MaxLength(15)]
        public string Phone { get; set; }

        /// <summary>
        /// Емаил
        /// </summary>
        [MaxLength(100)]
        //[EmailAddress]
        [RegularExpression(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$", ErrorMessage = "Укажите email в верном формате")]
        public string Email { get; set; }

        /// <summary>
        /// Год постройки
        /// </summary>
        //[Range(1800, 99999999999, ErrorMessage = "Минимальный год постройки - 1800")]
        public int? ConstructionYear { get; set; }

        /// <summary>
        /// Материал постройки
        /// </summary>
        [MaxLength(50)]
        public string ConstructionMaterial { get; set; }

        /// <summary>
        /// Этажность здания
        /// </summary>
        [Range(1, 100, ErrorMessage = "Кол-во этажей должно быть в пределах от 1 до 100")]
        public int? Floors { get; set; }

        /// <summary>
        /// Кол-во квартир
        /// </summary>
        public int? ApartCount { get; set; }

        /// <summary>
        /// Состояние дома: нормальный, ветхий или аварийный
        /// </summary>
        //[Required(ErrorMessage = "Укажите состояние дома")]
        public HouseStateCodes HouseStateCode { get; set; } = HouseStateCodes.NORMAL;

        /// <summary>
        /// Собственное отопление
        /// </summary>
        public bool? PersonalHeating { get; set; }

        /// <summary>
        /// Собственная гор.вода
        /// </summary>
        public bool? PersonalHotWater { get; set; }

        /// <summary>
        /// Собственное электроснабжение
        /// </summary>
        public bool? PersonalElectricPower { get; set; }

        /// <summary>
        /// Газифицирован
        /// </summary>
        public bool? Gasified { get; set; }

        /// <summary>
        /// Коэффициент начисления на нежилые помещения
        /// </summary>
        [Required(ErrorMessage = "Укажите коэффициент начисления на нежилые помещения")]
        [Range(100, 300, ErrorMessage = "Коэффициент должен быть в пределах от 100 до 300")]
        public int CoefUnlivingArea { get; set; }

        /// <summary>
        /// ФИО председателя
        /// </summary>
        [MaxLength(100)]
        public string Fio { get; set; }

        //OSI-213 
        /// <summary>
        /// Признак ОСИ\ПТ\КСК\Коднодинимум\TOO
        /// </summary>
        [Required(ErrorMessage = "Укажите тип объединения: 1-ОСИ, 2-ПТ, 3-Кондоминум, 4-КСК, 5-ТОО")]
        public int UnionTypeId { get; set; }

        /// <summary>
        /// Нужно начислять пеню или нет
        /// </summary>
        public bool CreateFine { get; set; }

        /// <summary>
        /// Тип регистрации: FULL/FREE, платная или бесплатная
        /// </summary>
        //[Required(ErrorMessage = "Укажите тип регистрации: FULL-платная, FREE-бесплатная")]
        public string RegistrationType { get; set; }
    }
}
