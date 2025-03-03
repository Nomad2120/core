using OSI.Core.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Banks
{
    /// <summary>
    /// Платежное поручение
    /// </summary>
    public class PaymentOrder
    {
        /// <summary>
        /// Дата
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// БИК банка
        /// </summary>
        public string BIC { get; set; }

        /// <summary>
        /// Расчетный счет поставщика услуги
        /// </summary>
        public string IBAN { get; set; }

        /// <summary>
        /// БИН поставщика услуги
        /// </summary>
        public string IDN { get; set; }

        /// <summary>
        /// Резидент + Сектор экономики
        /// </summary>
        public string KBE { get; set; }

        /// <summary>
        /// КНП (Код Назначения Платежа)
        /// </summary>
        public string KNP { get; set; }

        /// <summary>
        /// Назначение платежа
        /// </summary>
        public string Assign { get; set; }

        [SwaggerIgnore]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? ServiceId { get; set; }

        /// <summary>
        /// Название поставщика
        /// </summary>
        public string Name { get; set; }

        [SwaggerIgnore]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? OsiId { get; set; }

        /// <summary>
        /// Сумма
        /// </summary>
        public decimal Amount { get; set; }

        [SwaggerIgnore]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public decimal? BankCommission { get; set; }
    }
}
