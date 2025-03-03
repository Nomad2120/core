using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class AccountReportListItemDetail : ModelBase
    {
        [JsonIgnore]
        public int ItemId { get; set; }

        /// <summary>
        /// Сумма
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Комментарий
        /// </summary>
        [MinLength(6, ErrorMessage = "Минимальное количество символов в комментарии: 6")]
        [MaxLength(500)]
        public string Comment { get; set; }

        [Required(ErrorMessage = "Не указана категория")]
        public int? CategoryId { get; set; }

        [JsonIgnore]
        public virtual AccountReportListItem Item { get; set; }

        [JsonIgnore]
        public virtual AccountReportCategory Category { get; set; }
    }
}
