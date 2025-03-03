using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OSI.Core.Models.Reports.SaldoOnAllPeriod
{
    public class SaldoPeriodItem
    {
        public string ServiceName { get; set; }

        public string ServiceNameKz { get; set; }

        public decimal Begin { get; set; }

        public decimal Debet { get; set; }

        public decimal DebetWithoutFixes { get; set; }  // OSI-190

        public decimal SumOfFixes { get; set; }         // OSI-190

        public decimal SumOfAccurals { get; set; }

        public decimal SumOfFines { get; set; }         // OSI-313

        public decimal SumOfUnpaidFines { get; set; }   // OSI-313

        public decimal Kredit { get; set; }

        public decimal End { get; set; }

        //OSI-270
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<SaldoPeriodItemFix> Fixes { get; set; }
    }
}