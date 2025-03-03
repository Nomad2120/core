using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class ActOperation
    {
        [Key]
        public int Id { get; set; }

        public DateTime Dt { get; set; }

        public decimal Amount { get; set; }

        public int PaymentOrderId { get; set; }

        [JsonIgnore]
        public virtual PaymentOrder PaymentOrder { get; set; }

        public int ActId { get; set; }

        [JsonIgnore]
        public virtual Act Act { get; set; }
    }
}
