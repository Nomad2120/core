using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Reports
{
    public class OSVSummaryRow
    {
        public int Count { get; set; }
        public decimal Begin { get; set; }
        public decimal Debet { get; set; }
        public decimal Kredit { get; set; }
        public decimal End { get; set; }
    }
}
