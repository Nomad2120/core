using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Reports
{
    public class PaymentTransaction
    {
        public DateTime Dt { get; set; }
        public string AbonentName { get; set; }
        public string Flat { get; set; }
        public string ServiceName { get; set; }
        public decimal Amount { get; set; }
        public string BankName { get; set; }
    }
}
