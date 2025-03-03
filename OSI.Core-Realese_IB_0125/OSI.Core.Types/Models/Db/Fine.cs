using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class Fine : ModelBase
    {
        public decimal UnpaidAmount { get; set; }

        public int TransactionId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual Transaction Transaction { get; set; }
    }
}
