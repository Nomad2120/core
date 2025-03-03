using OSI.Core.Models.Responses;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class CreateFixRequest
    {
        /// <summary>
        /// Код абонента
        /// </summary>
        [Required]
        public int AbonentNum { get; set; }

        /// <summary>
        /// Причина корректировки
        /// </summary>
        [Required]
        [MinLength(7, ErrorMessage = "Минимальная длина строки: 7 символов")]
        [MaxLength(500, ErrorMessage = "Максимальная длина строки: 500 символов")]
        public string Reason { get; set; }

        /// <summary>
        /// Список услуг
        /// </summary>
        [Required]
        public List<FixService> Services { get; set; }
    }
}
