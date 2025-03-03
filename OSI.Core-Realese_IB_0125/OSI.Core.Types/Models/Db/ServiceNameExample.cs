using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class ServiceNameExample
    {
        [Key]
        [JsonIgnore]
        public int Id { get; set; }

        [JsonIgnore]
        public int ServiceGroupId { get; set; }

        [Required]
        [MaxLength(100)]
        public string NameRu { get; set; }

        [Required]
        [MaxLength(100)]
        public string NameKz { get; set; }

        [JsonIgnore]
        public virtual ServiceGroup ServiceGroup { get; set; }
    }
}
