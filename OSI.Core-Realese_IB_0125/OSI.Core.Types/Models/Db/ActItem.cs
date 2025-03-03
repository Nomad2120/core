using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class ActItem
    {
        [Key]
        [JsonIgnore]
        public int Id { get; set; }

        public string Description { get; set; }

        public DateTime DateWork { get; set; }

        public decimal Quantity { get; set; }

        public decimal Price { get; set; }

        public decimal Amount { get; set; }

        [JsonIgnore]
        public int ActId { get; set; }

        public string Note { get; set; }

        [JsonIgnore]
        public virtual Act Act { get; set; }
    }
}
