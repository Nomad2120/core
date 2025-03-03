using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace OSI.Core.Models.Db
{
    public class ReqType
    {
        [Key]
        [Required(ErrorMessage = "Укажите код", AllowEmptyStrings = false)]
        [MaxLength(50)]
        public string Code { get; set; }

        [Required(ErrorMessage = "Укажите код", AllowEmptyStrings = false)]
        [MaxLength(100)]
        public string Name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ReqDoc> ReqDocs { get; set; } = new HashSet<ReqDoc>();

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Registration> Registrations { get; set; } = new HashSet<Registration>();
    }
}
