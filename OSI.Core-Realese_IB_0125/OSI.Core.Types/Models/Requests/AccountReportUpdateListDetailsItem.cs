using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class AccountReportUpdateListDetailsItem
    {
        public int Id { get; set; }
        public int? CategoryId { get; set; }

        public IEnumerable<AccountReportListItemDetail> Details { get; set; }
    }
}
