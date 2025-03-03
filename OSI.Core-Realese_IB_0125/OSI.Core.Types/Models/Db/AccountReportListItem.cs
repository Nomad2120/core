using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    /// <summary>
    /// Запись об операции
    /// </summary>
    public class AccountReportListItem : ModelBase
    {
        [JsonIgnore]
        public int ListId { get; set; }

        /// <summary>
        /// Дата операции
        /// </summary>
        public DateTime Dt { get; set; }

        /// <summary>
        /// Сумма
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Получатель
        /// </summary>
        [MaxLength(500)]
        public string Receiver { get; set; }

        /// <summary>
        /// БИН
        /// </summary>
        [MaxLength(12)]
        public string ReceiverBin { get; set; }

        /// <summary>
        /// Получатель
        /// </summary>
        [MaxLength(500)]
        public string Sender { get; set; }

        /// <summary>
        /// БИН
        /// </summary>
        [MaxLength(12)]
        public string SenderBin { get; set; }

        /// <summary>
        /// Назначение
        /// </summary>
        [MaxLength(1000)]
        public string Assign { get; set; }

        /// <summary>
        /// Тип операции
        /// </summary>
        [JsonPropertyName("operationType")]
        public OperationTypeCodes OperationTypeCode { get; set; }

        [NotMapped]
        public string OperationTypeNameRu => OperationType?.NameRu;

        [NotMapped]
        public string OperationTypeNameKz => OperationType?.NameKz;

        public int? CategoryId { get; set; }

        [JsonIgnore]
        public virtual AccountReportList List { get; set; }

        [JsonIgnore]
        public virtual OperationType OperationType { get; set; }

        [JsonIgnore]
        public virtual AccountReportCategory Category { get; set; }

        /// <summary>
        /// Комментарии
        /// </summary>
        public virtual ICollection<AccountReportListItemDetail> Details { get; set; } = new HashSet<AccountReportListItemDetail>();
    }
}
