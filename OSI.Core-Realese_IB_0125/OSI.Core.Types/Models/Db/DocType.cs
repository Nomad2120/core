using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace OSI.Core.Models.Db
{
    public class DocType
    {
        public DocType()
        {
            AccountReportDocs = new HashSet<AccountReportDoc>();
            ActDocs = new HashSet<ActDoc>();
            OsiAccountApplicationDocs = new HashSet<OsiAccountApplicationDoc>();
            RegistrationDocs = new HashSet<RegistrationDoc>();
            ReqDocs = new HashSet<ReqDoc>();
            OsiDocs = new HashSet<OsiDoc>();
        }

        /// <summary>
        /// Код
        /// </summary>
        [Key]
        [Required(ErrorMessage = "Укажите код", AllowEmptyStrings = false)]
        [MaxLength(50)]
        public string Code { get; set; }

        /// <summary>
        /// Наименование на русском
        /// </summary>
        [Required(ErrorMessage = "Укажите название документа", AllowEmptyStrings = false)]
        [MaxLength(100)]
        public string NameRu { get; set; }

        /// <summary>
        /// Наименование на казахском
        /// </summary>
        [MaxLength(100)]
        public string NameKz { get; set; }

        /// <summary>
        /// Максимальный размер загружаемого файла
        /// </summary>
        [Required(ErrorMessage = "Укажите максимальный размер в байтах")]
        public int MaxSize { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<RegistrationDoc> RegistrationDocs { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ReqDoc> ReqDocs { get; set; }

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
