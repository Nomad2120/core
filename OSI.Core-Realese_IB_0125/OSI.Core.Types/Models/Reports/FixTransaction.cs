using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Reports
{
    public class FixTransaction
    {
        public DateTime Dt { get; set; }
        public string AbonentName { get; set; }
        public string Flat { get; set; }
        public string ServiceName { get; set; }
        public string ServiceGroupName { get; set; }
        public string Reason { get; set; }
        public decimal Amount { get; set; }
    }
}
