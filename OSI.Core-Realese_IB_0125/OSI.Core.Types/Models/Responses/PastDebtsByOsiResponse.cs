using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class PastDebtsByOsiResponse
    {
        public int AbonentId { get; set; }

        public string AbonentName { get; set; }

        public string Flat { get; set; }

        public IEnumerable<PastDebtsByOsiResponseItem> ServiceGroups { get; set; }
    }

    public class PastDebtsByOsiResponseItem
    {
        public int ServiceGroupId { get; set; }

        public string ServiceGroupNameRu { get; set; }

        public string ServiceGroupNameKz { get; set; }

        public IEnumerable<PastDebtInfo> PastDebts { get; set; }
    }
}
