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
    public class Payment : ModelBase
    {
        public Payment()
        {
             Transactions = new HashSet<Transaction>();
        }

        [MaxLength(100)]
        public string Reference { get; set; }

        [MaxLength(10)]
        public string BankCode { get; set; }

        [MaxLength(20)]
        public string State { get; set; }

        public DateTime RegistrationDate { get; set; }

        public DateTime? ProvDate { get; set; }

        public decimal Amount { get; set; }

        public decimal Comission { get; set; }

        public int? UserId { get; set; }

        public int ContractId { get; set; }

        public int OsiId { get; set; }

        public int? PaymentOrderId { get; set; }

        [MaxLength(20)]
        public string AbonentNum { get; set; }

        [JsonIgnore]
        public virtual User User { get; set; }

        [JsonIgnore]
        public virtual Contract Contract { get; set; }

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        [JsonIgnore]
        public virtual PaymentOrder PaymentOrder { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
