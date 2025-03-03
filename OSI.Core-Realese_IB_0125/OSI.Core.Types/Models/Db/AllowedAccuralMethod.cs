using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class AllowedAccuralMethod
    {
        [Key]
        public int Id { get; set; }

        public int ServiceGroupId { get; set; }

        public int AccuralMethodId { get; set; }

        [JsonIgnore]
        public virtual ServiceGroup ServiceGroup { get; set; }

        [JsonIgnore]
        public virtual AccuralMethod AccuralMethod { get; set; }
    }
}
