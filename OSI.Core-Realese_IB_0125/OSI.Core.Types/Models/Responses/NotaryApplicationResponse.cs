using OSI.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class NotaryApplicationResponse
    {
        public int OsiId { get; set; }
        public string OsiName { get; set; }
        public string OsiIdn { get; set; }
        public string OsiAddress { get; set; }
        public string OsiChairman { get; set; }
        public string OsiPhone { get; set; }
        public string AbonentName { get; set; }
        public string AbonentIdn { get; set; }
        public string AbonentFlat { get; set; }
        public string AbonentAddress { get; set; }
        public string AbonentPhone { get; set; }
        public DateTime DebtDate { get; set; }
        public decimal TotalDebt => Registry.Sum(g => g.TotalDebt);
        public string TotalDebtString => TotalDebt.RusSpelledOut(true, true);
        public List<NotaryApplicationRegistryGroup> Registry { get; set; } = new List<NotaryApplicationRegistryGroup>();
    }

    public class NotaryApplicationRegistryGroup
    {
        public string ServiceName { get; set; }
        public string ServiceNameKz { get; set; }
        public decimal TotalDebt => Debts.Sum(g => g.Debt);
        public string TotalDebtString => TotalDebt.RusSpelledOut(true, true);
        public List<NotaryApplicationRegistryItem> Debts { get; set; } = new List<NotaryApplicationRegistryItem>();
    }

    public class NotaryApplicationRegistryItem
    {
        public int Number { get; set; }
        public string Period { get; set; }
        public decimal Debt { get; set; }
        public decimal CumulativeDebt { get; set; }
    }
}
