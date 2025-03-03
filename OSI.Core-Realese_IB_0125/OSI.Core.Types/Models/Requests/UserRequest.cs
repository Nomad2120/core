using OSI.Core.Validators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class UserRequest
    {
        /// <summary>
        /// ИИН
        /// </summary>
        [MaxLength(12)]
        [IIN]
        public string IIN { get; set; }

        /// <summary>
        /// ФИО
        /// </summary>
        [MaxLength(100)]
        public string FIO { get; set; }

        /// <summary>
        /// Телефон пользователя
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
    }
}
