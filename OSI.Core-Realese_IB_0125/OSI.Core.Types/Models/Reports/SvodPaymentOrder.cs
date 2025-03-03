using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Reports
{
    public class SvodPaymentOrder
    {
        public string BankName { get; set; }

        public string IBAN { get; set; }

        public DateTime Date { get; set; }

        public decimal Amount { get; set; }

        public decimal ComisBank { get; set; }

        public decimal ComisOur { get; set; }

        public decimal AmountToTransfer { get; set; }
    }
}
