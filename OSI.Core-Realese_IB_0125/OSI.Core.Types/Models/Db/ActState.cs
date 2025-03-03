using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class ActState
    {
        public ActState()
        {
            Acts = new HashSet<Act>();
        }

        [Key]
        public ActStateCodes Code { get; set; }

        [Required(ErrorMessage = "Укажите расшифровку", AllowEmptyStrings = false)]
        [MaxLength(100)]
        public string Name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Act> Acts { get; set; }
    }
}
