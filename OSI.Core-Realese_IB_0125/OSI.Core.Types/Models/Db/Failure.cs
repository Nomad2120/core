using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class Failure: ModelBase
    {
        public int OsiId { get; set; }

        public int ServiceGroupId { get; set; }

        public decimal Amount { get; set; }

        public DateTime Dt { get; set; }

        public int? PaymentOrderId { get; set; }

        public Osi Osi { get; set; }

        public ServiceGroup ServiceGroup { get; set; }

        public PaymentOrder PaymentOrder { get; set; }
    }
}
