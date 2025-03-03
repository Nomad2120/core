using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class UnionType : ModelBase
    {
        public UnionType()
        {
            Registrations = new HashSet<Registration>();
            Osies = new HashSet<Osi>();
            ReqDocs = new HashSet<ReqDoc>();
        }

        public string NameRu { get; set; }

        [MaxLength(100)]
        public string NameKz { get; set; }

        [JsonIgnore]
        [MinLength(2)]
        [MaxLength(2)]
        public string Kbe { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Registration> Registrations { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Osi> Osies { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ReqDoc> ReqDocs { get; set; }
    }
}
