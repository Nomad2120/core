using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.AccountReports
{
    public class BankStatementItem
    {
        public DateTime Dt { get; set; }

        public decimal Amount { get; set; }

        public string Receiver { get; set; }

        public string Sender { get; set; }

        public string Assign { get; set; }

        public string ReceiverBin { get; set; }

        public string SenderBin { get; set; }

        public OperationTypeCodes OperationTypeCode { get; set; }
    }
}
