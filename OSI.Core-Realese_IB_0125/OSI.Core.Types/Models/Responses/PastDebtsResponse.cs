using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class PastDebtsResponse
    {
        public decimal Saldo { get; set; }

        public IEnumerable<PastDebtInfo> PastDebts { get; set; }
    }
}
