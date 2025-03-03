using ESoft.CommonLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OSI.Core.Validators
{
    public class IINAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        public bool AllowBIN { get; set; } = true;
        public bool AllowOnlyBIN { get; set; } = false;

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            string iin = value?.ToString();
            if (string.IsNullOrEmpty(iin))
                return null;
            if (!Regex.IsMatch(iin, @"^\d{12}$"))
                return new ValidationResult($"Укажите {(AllowOnlyBIN ? "Б" : (AllowBIN ? "ИИН/Б" : "И"))}ИН в виде 12 цифр");
            var isCorrect = IINChecker.CheckIIN(iin, out bool isBin);
            if (isCorrect)
            {
                if (isBin && !AllowBIN && !AllowOnlyBIN)
                    return new ValidationResult("БИН не разрешен для ввода");
                if (!isBin && AllowOnlyBIN)
                    return new ValidationResult("ИИН не разрешен для ввода");
                return null;
            }
            else
            {
                return new ValidationResult($"Введенный {(AllowOnlyBIN ? "Б" : (AllowBIN ? (isBin ? "Б" : "И") : "И"))}ИН некорректен");
            }
        }
    }
}
