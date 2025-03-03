using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class OperationType
    {

        /// <summary>
        /// Код
        /// </summary>
        [Key]
        public OperationTypeCodes Code { get; set; }

        /// <summary>
        /// Наименование на русском
        /// </summary>
        [MaxLength(100)]
        public string NameRu { get; set; }

        /// <summary>
        /// Наименование на казахском
        /// </summary>
        [MaxLength(100)]
        public string NameKz { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<AccountReportCategoryOption> AccountReportCategoryOptions { get; set; } = new HashSet<AccountReportCategoryOption>();

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<AccountReportListItem> AccountReportListItems { get; set; } = new HashSet<AccountReportListItem>();
    }
}
