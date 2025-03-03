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
    /// Список по счету
    /// </summary>
    public class AccountReportList : ModelBase
    {
        /// <summary>
        /// Счет
        /// </summary>
        [MaxLength(20)]
        public string Account { get; set; }

        /// <summary>
        /// Тип счета
        /// </summary>
        [JsonPropertyName("accountType")]
        public AccountTypeCodes AccountTypeCode { get; set; }

        [NotMapped]
        public string AccountTypeNameRu => AccountType?.NameRu;

        [NotMapped]
        public string AccountTypeNameKz => AccountType?.NameKz;

        /// <summary>
        /// Период
        /// </summary>
        public DateTime Period { get; set; }

        public bool IsFilled { get; set; } = false;

        /// <summary>
        /// БИК банка
        /// </summary>
        public string Bic { get; set; }

        [NotMapped]
        public string BankName => Bank?.Name;

        [NotMapped]
        public string BankStatementVideoUrl => Bank?.StatementVideoUrl;

        /// <summary>
        /// Сальдо на начало
        /// </summary>
        public decimal Begin { get; set; }

        public decimal Debet { get; set; }

        public decimal Kredit { get; set; }

        /// <summary>
        /// Сальдо на конец
        /// </summary>
        public decimal End { get; set; }

        [JsonIgnore]
        [NotMapped]
        public IEnumerable<AccountReport> Reports => Relations.Select(r => r.Report);

        [NotMapped]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool? IsInPublishedReport { get; set; }

        [JsonIgnore]
        public virtual ICollection<AccountReportListRelation> Relations { get; set; } = new HashSet<AccountReportListRelation>();

        [JsonIgnore]
        public virtual AccountType AccountType { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(Bic))]
        public virtual Bank Bank { get; set; }

        /// <summary>
        /// Записи об операциях
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<AccountReportListItem> Items { get; set; } = new HashSet<AccountReportListItem>();

        [JsonPropertyName("items")]
        [NotMapped]
        public IEnumerable<AccountReportListItem> DisplayItems => Items.OrderBy(i => i.Dt);
    }
}
