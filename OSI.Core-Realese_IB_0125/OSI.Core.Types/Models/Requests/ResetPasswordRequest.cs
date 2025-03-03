using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Укажите временный код (ОТП)")]
        public string Otp { get; set; }

        [Required(ErrorMessage = "Укажите новый пароль")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Подтвердите новый пароль")]
        public string ConfirmPassword { get; set; }
    }
}
