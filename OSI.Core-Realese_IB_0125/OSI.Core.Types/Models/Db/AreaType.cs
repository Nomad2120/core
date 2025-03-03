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
    public class AreaType
    {
        public AreaType()
        {
            Abonents = new HashSet<Abonent>();
        }

        /// <summary>
        /// Код
        /// </summary>
        [Key]
        public AreaTypeCodes Code { get; set; }

        /// <summary>
        /// Наименование на русском
        /// </summary>
        [MaxLength(100)]
        public string NameRu { get; set; }

        /// <summary>
        /// Наименование на казахском
        /// </summary>
        [MaxLength(100)]
        public string NameKz { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Abonent> Abonents { get; set; }
    }
}
