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
    public class ServiceCompany
    {
        public ServiceCompany()
        {
            OsiServiceCompanies = new HashSet<OsiServiceCompany>();
        }

        /// <summary>
        /// Код
        /// </summary>
        [Key]
        public string Code { get; set; }

        /// <summary>
        /// Наименование на русском
        /// </summary>
        [MaxLength(200)]
        public string NameRu { get; set; }

        /// <summary>
        /// Наименование на казахском
        /// </summary>
        [MaxLength(200)]
        public string NameKz { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiServiceCompany> OsiServiceCompanies { get; set; }
    }
}
