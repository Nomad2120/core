using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class Fix : ModelBase
    {
        public string Reason { get; set; }

        public DateTime RegistrationDate { get; set; }

        public decimal Amount { get; set; }

        public int UserId { get; set; }

        public int OsiId { get; set; }

        public virtual User User { get; set; }

        public virtual Osi Osi { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
