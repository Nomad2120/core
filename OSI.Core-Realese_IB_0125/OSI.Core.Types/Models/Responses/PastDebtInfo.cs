using OSI.Core.DataAnnotations;
using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class PastDebtInfo
    {
        [RequiredExt]
        public DateTime Period { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public PastDebtInfo() { }

        public PastDebtInfo(PastDebt pastDebt)
        {
            Period = pastDebt.Period;
            Amount = pastDebt.Amount;
        }
    }
}
