using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class Transaction
    {
        public Transaction()
        {
            ServiceGroupSaldos = new HashSet<ServiceGroupSaldo>();
        }

        [Key]
        public int Id { get; set; }

        public DateTime Dt { get; set; }

        public TransactionTypeCodes TransactionType { get; set; }

        public int OsiId { get; set; }

        public int AbonentId { get; set; }

        public int GroupId { get; set; }
                
        public int? OsiServiceId { get; set; }

        public int? PlanAccuralId { get; set; }

        public int? PaymentId { get; set; }

        public int? FixId { get; set; }

        public decimal Amount { get; set; }

        [NotMapped]
        public string ServiceNameRu => OsiService?.NameRu;

        [NotMapped]
        public string ServiceNameKz => OsiService?.NameKz;

        [NotMapped]
        public string GroupNameRu => Group?.NameRu;

        [NotMapped]
        public string GroupNameKz => Group?.NameKz;

        public int? PaymentOrderId { get; set; }

        public int? OsiServiceAmountId { get; set; }

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        [JsonIgnore]
        public virtual ServiceGroup Group { get; set; }

        [JsonIgnore]
        public virtual OsiService OsiService { get; set; }

        [JsonIgnore]
        public virtual Abonent Abonent { get; set; }

        [JsonIgnore]
        public virtual PlanAccural PlanAccural { get; set; }

        [JsonIgnore]
        public virtual Payment Payment { get; set; }

        [JsonIgnore]
        public virtual Fix Fix { get; set; }

        [JsonIgnore]
        public virtual Fine Fine { get; set; }

        [JsonIgnore]
        public virtual PaymentOrder PaymentOrder { get; set; }

        [JsonIgnore]
        public virtual OsiServiceAmount OsiServiceAmount { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ServiceGroupSaldo> ServiceGroupSaldos { get; set; }
    }
}
