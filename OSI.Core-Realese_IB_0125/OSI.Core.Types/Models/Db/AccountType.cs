using OSI.Core.Models.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace OSI.Core.Models.Db
{
    public class AccountType
    {
        public AccountType()
        {
            AccountReportCategoryOptions = new HashSet<AccountReportCategoryOption>();
            AccountReportLists = new HashSet<AccountReportList>();
            OsiAccounts = new HashSet<OsiAccount>();
            OsiAccountApplications = new HashSet<OsiAccountApplication>();
            RegistrationAccounts = new HashSet<RegistrationAccount>();
            ServiceGroups = new HashSet<ServiceGroup>();
        }

        /// <summary>
        /// Код
        /// </summary>
        [Key]
        public AccountTypeCodes Code { get; set; }

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
        public virtual ICollection<ServiceGroup> ServiceGroups { get; set; }

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
        public virtual ICollection<AccountReportCategoryOption> AccountReportCategoryOptions { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<RegistrationAccount> RegistrationAccounts { get; set; }
    }
}
