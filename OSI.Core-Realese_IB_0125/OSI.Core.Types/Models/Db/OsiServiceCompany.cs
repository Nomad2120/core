using OSI.Core.Models.Requests;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OSI.Core.Models.Db
{
    public class OsiServiceCompany: OsiServiceCompanyRequest
    {
        [Key]
        public int Id { get; set; }

        [NotMapped]
        public string ServiceCompanyNameRu => ServiceCompany?.NameRu;

        [NotMapped]
        public string ServiceCompanyNameKz => ServiceCompany?.NameKz;

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        [JsonIgnore]
        public virtual ServiceCompany ServiceCompany { get; set; }
    }
}
