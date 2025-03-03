using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.AccountReports
{
    public class BankStatement
    {
        public string Account { get; set; }

        public string Bic { get; set; }

        public DateTime PeriodStart { get; set; }    
        
        public DateTime PeriodEnd { get; set; }

        public decimal Begin { get; set; }

        public decimal Debet { get; set; }

        public decimal Kredit { get; set; }

        public decimal End { get; set; }

        public List<BankStatementItem> Items { get; set; } = new List<BankStatementItem>();
    }
}
