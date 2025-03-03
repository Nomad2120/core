using OSI.Core.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace OSI.Core.Models.Requests
{
    public class OsiServiceCompanyRequest
    {
        /// <summary>
        /// Телефоны компаний
        /// </summary>
        [Required(ErrorMessage = "Укажите телефоны сервисных компаний")]
        public string Phones { get; set; }

        /// <summary>
        /// Адреса компаний
        /// </summary>
        public string Addresses { get; set; }

        /// <summary>
        /// Объект ОСИ
        /// </summary>
        [Required(ErrorMessage = "Укажите ОСИ")]
        public int OsiId { get; set; }

        /// <summary>
        /// Услуга
        /// </summary>
        [Required(ErrorMessage = "Укажите сервисную компанию")]
        public string ServiceCompanyCode { get; set; }

        /// <summary>
        /// Показывать телефон на квитанции
        /// </summary>
        public bool ShowPhones { get; set; }
    }
}
