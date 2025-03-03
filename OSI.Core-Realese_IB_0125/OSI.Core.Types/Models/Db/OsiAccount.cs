using OSI.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class OsiAccount : OsiAccountRequest
    {
        [Key]
        public int Id { get; set; }

        [NotMapped]
        public string BankName => Bank?.Name;

        [NotMapped]
        public string AccountTypeNameRu => AccountType?.NameRu;

        [NotMapped]
        public string AccountTypeNameKz => AccountType?.NameKz;

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        [JsonIgnore]
        public virtual Bank Bank { get; set; }

        [JsonIgnore]
        public virtual AccountType AccountType { get; set; }

        [JsonIgnore]
        public virtual ServiceGroup ServiceGroup { get; set; }

        [JsonIgnore]
        public virtual ICollection<OsiAccountApplication> OsiAccountApplications { get; set; }
    }
}
