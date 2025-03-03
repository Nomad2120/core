using OSI.Core.Validators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class Scan : ModelBase
    {
        public Scan()
        {
            AccountReportDocs = new HashSet<AccountReportDoc>();
            ActDocs = new HashSet<ActDoc>();
            OsiAccountApplicationDocs = new HashSet<OsiAccountApplicationDoc>();
            RegistrationDocs = new HashSet<RegistrationDoc>();
            OsiDocs = new HashSet<OsiDoc>();
        }

        //[Required(ErrorMessage = "Загрузите изображение")]
        //[MaxLength(1024 * 1024 * 2)] // 2 Mbyte
        //public byte[] Photo { get; set; }

        [Required(ErrorMessage = "Загрузите изображение")]
        [MaxLength(100)]
        public string FileName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<RegistrationDoc> RegistrationDocs { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiDoc> OsiDocs { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ActDoc> ActDocs { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<AccountReportDoc> AccountReportDocs { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiAccountApplicationDoc> OsiAccountApplicationDocs { get; set; }
    }
}
