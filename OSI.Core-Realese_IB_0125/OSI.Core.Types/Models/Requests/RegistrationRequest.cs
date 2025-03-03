using OSI.Core.Validators;
using System;
using System.ComponentModel.DataAnnotations;

namespace OSI.Core.Models.Requests
{
    public class RegistrationRequest
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
        /// Адрес
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите адрес")]
        [MaxLength(100, ErrorMessage = "Максимальная длина строки: 100 символов")]
        public string Address { get; set; }

        /// <summary>
        /// Пользователь, подавший заявку
        /// </summary>
        [Required(ErrorMessage = "Укажите пользователя")]
        public int UserId { get; set; }

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
        //[Required(ErrorMessage = "Укажите емаил")]
        [MaxLength(100, ErrorMessage = "Максимальная длина строки: 100 символов")]
        //[EmailAddress]
        [RegularExpression(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$", ErrorMessage = "Укажите email в верном формате")]
        public string Email { get; set; }

        /// <summary>
        /// Кол-во квартир
        /// </summary>
        [Required(ErrorMessage = "Укажите кол-во квартир")]
        [Range(1, 999)]
        public int ApartCount { get; set; }

        /// <summary>
        /// ФИО председателя
        /// </summary>
        [MaxLength(100)]
        public string Fio { get; set; }

        /// <summary>
        /// ID адреса из адресного регистра
        /// </summary>
        public int? AddressRegistryId { get; set; }

        /// <summary>
        /// РКА из адресного регистра
        /// </summary>
        [MaxLength(16)]
        public string Rca { get; set; }

        /// <summary>
        /// AtsId
        /// </summary>
        public int? AtsId { get; set; }

        //OSI-213 
        /// <summary>
        /// Признак ОСИ\ПТ\КСК\Коднодинимум\TOO
        /// </summary>
        public int UnionTypeId { get; set; }

        /// <summary>
        /// Тип заявки: FULL/FREE, платная или бесплатная
        /// </summary>
        //[Required(ErrorMessage = "Укажите тип заявки: FULL-платная, FREE-бесплатная")]
        public string RegistrationType { get; set; }
    }
}
