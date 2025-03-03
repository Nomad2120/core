using System;

namespace OSI.Core.Models.Reports.SaldoOnAllPeriod
{
    public class SaldoPeriodItemFix
    {
        public DateTime Dt { get; set; }
        public string Reason { get; set; }
        public decimal Amount { get; set; }
    }
}