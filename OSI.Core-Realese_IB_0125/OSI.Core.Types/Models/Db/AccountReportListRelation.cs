using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class AccountReportListRelation : ModelBase
    {
        public int ReportId { get; set; }

        public int ListId { get; set; }

        public virtual AccountReport Report { get; set; }

        public virtual AccountReportList List { get; set; }
    }
}
