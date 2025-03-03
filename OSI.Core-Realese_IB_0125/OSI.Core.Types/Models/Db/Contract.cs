using OSI.Core.Models.Enums;
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
    public class Contract : ModelBase
    {
        public Contract()
        {
            Payments = new HashSet<Payment>();
            PaymentOrders = new HashSet<PaymentOrder>();
        }

        [Required]
        [MaxLength(200)]
        public string BankName { get; set; }
        
        [Required]
        [MaxLength(10)]
        public string BankCode { get; set; }
        
        public string IpList { get; set; }

        [Required]
        public decimal Comission { get; set; }

        public ComissionCalcTypes ComissionCalcType { get; set; } = ComissionCalcTypes.EachPaymentToEven;

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Payment> Payments { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<PaymentOrder> PaymentOrders { get; set; }
    }
}

