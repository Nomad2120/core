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
    public class OsiServiceAmount
    {
        public OsiServiceAmount()
        {
            Transactions = new HashSet<Transaction>();
        }

        [Key]
        public int Id { get; set; }

        public int OsiId { get; set; }

        public int OsiServiceId { get; set; }

        public DateTime Dt { get; set; }

        public int AccuralMethodId { get; set; }

        public decimal Amount { get; set; }

        public string Note { get; set; }

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        [JsonIgnore]
        public virtual OsiService OsiService { get; set; }

        [JsonIgnore]
        public virtual AccuralMethod AccuralMethod { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
