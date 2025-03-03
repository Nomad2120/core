using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class Arendator
    {
        [Key]
        public int Id { get; set; }

        public int AbonentId { get; set; }

        public string Address { get; set; }

        [Required]
        public string Rca { get; set; }

        [JsonIgnore]
        public virtual Abonent Abonent { get; set; }
    }
}
