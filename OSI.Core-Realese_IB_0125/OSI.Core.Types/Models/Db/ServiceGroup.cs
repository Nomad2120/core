using OSI.Core.Models.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace OSI.Core.Models.Db
{
    public class ServiceGroup
    {
        public ServiceGroup()
        {
            AllowedAccuralMethods = new HashSet<AllowedAccuralMethod>();
            Failures = new HashSet<Failure>();
            OsiAccounts = new HashSet<OsiAccount>();
            OsiAccountApplications = new HashSet<OsiAccountApplication>();
            OsiServices = new HashSet<OsiService>();
            PastDebts = new HashSet<PastDebt>();
            PaymentOrders = new HashSet<PaymentOrder>();
            RegistrationAccounts = new HashSet<RegistrationAccount>();
            ServiceGroupSaldos = new HashSet<ServiceGroupSaldo>();
            ServiceNameExamples = new HashSet<ServiceNameExample>();
            Transactions = new HashSet<Transaction>();
        }

        [Key]
        public int Id { get; set; }

        public string NameRu { get; set; }

        public string NameKz { get; set; }

        public AccountTypeCodes AccountTypeCode { get; set; }

        public string Code { get; set; }

        public bool CanChangeName { get; set; }

        /// <summary>
        /// OSI-163 услугу можно подключать только в ед.экземпляре
        /// </summary>
        public bool JustOne { get; set; }

        /// <summary>
        /// OSI-164 Целевой взнос начисляется один раз, после начисления отключается.
        /// </summary>
        public bool CopyToNextPeriod { get; set; }

        public bool CanEditAbonents { get; set; }

        public bool CanCreateFixes { get; set; }

        [NotMapped]
        public string AccountTypeNameRu => AccountType?.NameRu;

        [NotMapped]
        public string AccountTypeNameKz => AccountType?.NameKz;

        [JsonIgnore]
        public virtual AccountType AccountType { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Transaction> Transactions { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<PaymentOrder> PaymentOrders { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<PastDebt> PastDebts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ServiceGroupSaldo> ServiceGroupSaldos { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Failure> Failures { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiService> OsiServices { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<AllowedAccuralMethod> AllowedAccuralMethods { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ServiceNameExample> ServiceNameExamples { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiAccount> OsiAccounts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiAccountApplication> OsiAccountApplications { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<RegistrationAccount> RegistrationAccounts { get; set; }
    }
}
