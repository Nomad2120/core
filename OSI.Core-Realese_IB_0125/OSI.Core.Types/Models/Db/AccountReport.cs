using IbanNet.DataAnnotations;
using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    /// <summary>
    /// Отчет председателя перед жильцами
    /// </summary>
    public class AccountReport : ModelBase
    {
        /// <summary>
        /// Id ОСИ
        /// </summary>
        public int OsiId { get; set; }

        /// <summary>
        /// Состояние отчета
        /// </summary>
        public AccountReportStateCodes State { get; set; }

        /// <summary>
        /// Период
        /// </summary>
        public DateTime Period { get; set; }

        /// <summary>
        /// Дата публикации
        /// </summary>
        public DateTime? PublishDate { get; set; }

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        /// <summary>
        /// Списки по счетам
        /// </summary>
        [NotMapped]
        public IEnumerable<AccountReportList> Lists => ListRelations.Select(lr => lr.List);

        [JsonIgnore]
        public virtual ICollection<AccountReportListRelation> ListRelations { get; set; } = new HashSet<AccountReportListRelation>();

        public virtual ICollection<AccountReportDoc> Docs { get; set; } = new HashSet<AccountReportDoc>();
    }
}
