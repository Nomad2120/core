using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class PastDebt : ModelBase
    {
        public int AbonentId { get; set; }

        public int ServiceGroupId { get; set; }

        public DateTime Period { get; set; }

        public decimal Amount { get; set; }

        [JsonIgnore]
        public virtual Abonent Abonent { get; set; }

        [JsonIgnore]
        public virtual ServiceGroup ServiceGroup { get; set; }
    }
}
