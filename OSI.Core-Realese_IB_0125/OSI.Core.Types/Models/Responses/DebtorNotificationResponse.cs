using OSI.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class DebtorNotificationResponse
    {
        public int OsiId { get; set; }

        public string Flat { get; set; }

        public string Address { get; set; }

        public string OsiName { get; set; }

        public string OsiChairman { get; set; }

        public DateTime DebtDate { get; set; }

        public IEnumerable<DebtorNotificationResponseItem> ServicesDebts { get; set; }
    }

    public class DebtorNotificationResponseItem
    {
        public string ServiceName { get; set; }

        public string ServiceNameKz { get; set; }

        public decimal Saldo { get; set; }

        public string SaldoString => Saldo.RusSpelledOut(true, true);
    }
}
