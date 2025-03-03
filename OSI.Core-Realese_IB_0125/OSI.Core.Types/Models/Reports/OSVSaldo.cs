using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Reports
{
    public class OSVSaldo
    {
        [JsonIgnore]
        public int ServiceGroupId { get; set; }
        public string ServiceName { get; set; }
        public string ServiceNameKz { get; set; }
        public decimal Begin { get; set; }
        public decimal Debet { get; set; }
        public decimal DebetWithoutFixes { get; set; }  // OSI-190
        public decimal SumOfFixes { get; set; }         // OSI-190
        public decimal SumOfAccurals { get; set; }
        public decimal SumOfFines { get; set; }         // OSI-313
        public decimal Kredit { get; set; }
        public decimal End { get; set; }
    }
}
