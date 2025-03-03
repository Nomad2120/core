using OSI.Core.Models.Requests;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OSI.Core.Models.Db
{
    public class RegistrationAccount: RegistrationAccountRequest
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
        public virtual Registration Registration { get; set; }

        [JsonIgnore]
        public virtual Bank Bank { get; set; }

        [JsonIgnore]
        public virtual AccountType AccountType { get; set; }

        [JsonIgnore]
        public virtual ServiceGroup ServiceGroup { get; set; }
    }
}
