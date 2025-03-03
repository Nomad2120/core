using ESoft.CommonLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OSI.Core.Validators
{
    public class ValuesAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        private readonly List<string> allowedValues;

        public ValuesAttribute(params string[] allowedValues)
        {
            this.allowedValues = new(allowedValues)
            {
                ""
            };
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            string v = value?.ToString();
            if (v == null || allowedValues.Contains(v))
            {
                return null;
            }
            else
            {
                return new ValidationResult($"Допустимы только следующие значения: {allowedValues.Select(x => x.Quote("\"")).StringJoin(", ")}");
            }
        }
    }
}
