using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace OSI.Core.Models.Db
{
    public class Bank
    {
        public Bank()
        {
            AccountReportLists = new HashSet<AccountReportList>();
            OsiAccounts = new HashSet<OsiAccount>();
            OsiAccountApplications = new HashSet<OsiAccountApplication>();
            RegistrationAccounts = new HashSet<RegistrationAccount>();
        }

        [Key]
        [MaxLength(10)]
        public string Bic { get; set; }

        [MaxLength(200)]
        public string Name { get; set; }

        // https://www.nationalbank.kz/ru/page/spravochnik-bik-rk-ps
        [MaxLength(10)]
        public string Identifier { get; set; }

        [MaxLength(200)]
        [JsonIgnore]
        public string StatementVideoUrl { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiAccount> OsiAccounts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiAccountApplication> OsiAccountApplications { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<AccountReportList> AccountReportLists { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<RegistrationAccount> RegistrationAccounts { get; set; }
    }
}
