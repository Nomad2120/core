using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class AccountReportCategory : ModelBase
    {
        [JsonIgnore]
        public int? ParentId { get; set; }

        [MaxLength(10)]
        public string Number { get; set; }

        [MaxLength(500)]
        public string NameRu { get; set; }
    
        [MaxLength(500)]
        public string NameKz { get; set; }

        public virtual AccountReportCategory Parent { get; set; }

        public virtual ICollection<AccountReportCategory> SubCategories { get; set; } = new HashSet<AccountReportCategory>();

        public virtual ICollection<AccountReportCategoryOption> Options { get; set; } = new HashSet<AccountReportCategoryOption>();

        public virtual ICollection<AccountReportListItem> AccountReportListItems { get; set; } = new HashSet<AccountReportListItem>();

        public virtual ICollection<AccountReportListItemDetail> AccountReportListItemDetails { get; set; } = new HashSet<AccountReportListItemDetail>();
    }
}
