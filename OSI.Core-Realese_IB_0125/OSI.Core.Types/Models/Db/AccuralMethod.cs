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
    public class AccuralMethod
    {
        public AccuralMethod()
        {
            OsiServiceAmounts = new HashSet<OsiServiceAmount>();
            AllowedAccuralMethods = new HashSet<AllowedAccuralMethod>();
        }

        [Key]
        public int Id { get; set; }

        [JsonIgnore]
        public string Code { get; set; }

        [JsonPropertyName("descriptionRu")]
        public string Description { get; set; }

        public string DescriptionKz { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiServiceAmount> OsiServiceAmounts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<AllowedAccuralMethod> AllowedAccuralMethods { get; set; }
    }
}
