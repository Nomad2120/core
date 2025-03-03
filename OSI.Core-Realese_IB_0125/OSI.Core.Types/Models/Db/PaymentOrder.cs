using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class PaymentOrder
    {
        public PaymentOrder()
        {
            ActOperations = new HashSet<ActOperation>();
            Payments = new HashSet<Payment>();
            Transactions = new HashSet<Transaction>();
            Failures = new HashSet<Failure>();
        }

        [Key]
        public int Id { get; set; }

        public DateTime DtReg { get; set; }

        public int OsiId { get; set; }

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        [Required]
        [MaxLength(8)]
        public string Bic { get; set; }

        [Required]
        [MaxLength(20)]
        public string Account { get; set; }

        [Required]
        [MaxLength(12)]
        public string Idn { get; set; }

        [Required]
        [MaxLength(2)]
        public string Kbe { get; set; }

        [Required]
        [MaxLength(3)]
        public string Knp { get; set; }

        [MaxLength(100)]
        public string Assign { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public decimal Amount { get; set; }

        public decimal ComisOur { get; set; }

        public decimal ComisBank { get; set; }

        public decimal ComisFail { get; set; }

        public int CountPayments { get; set; }

        public int ContractId { get; set; }

        public int? ServiceGroupId { get; set; }

        [NotMapped]
        public string BankName => Contract?.BankName;

        [NotMapped]
        public decimal AmountToTransfer => Amount - ComisBank - ComisOur;

        [JsonIgnore]
        public virtual Contract Contract { get; set; }

        [JsonIgnore]
        public virtual ServiceGroup ServiceGroup { get; set; }

        [Required]
        [MaxLength(10)]
        public string State { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Payment> Payments { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ActOperation> ActOperations { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Transaction> Transactions { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Failure> Failures { get; set; }

    }
}
