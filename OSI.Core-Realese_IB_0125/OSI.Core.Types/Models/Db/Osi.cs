using OSI.Core.Validators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using OSI.Core.Models.Requests;
using System.ComponentModel.DataAnnotations.Schema;

namespace OSI.Core.Models.Db
{
    [Table("osi")]
    public class Osi: OsiRequest
    {
        public Osi()
        {
            Abonents = new HashSet<Abonent>();
            AccountReports = new HashSet<AccountReport>();
            Acts = new HashSet<Act>();
            ConnectedServices = new HashSet<ConnectedService>();
            Failures = new HashSet<Failure>();
            Fixes = new HashSet<Fix>();
            OsiAccountApplications = new HashSet<OsiAccountApplication>();
            OsiAccounts = new HashSet<OsiAccount>();
            OsiDocs = new HashSet<OsiDoc>();
            OsiServiceAmounts = new HashSet<OsiServiceAmount>();
            OsiServiceCompanies = new HashSet<OsiServiceCompany>();
            OsiServices = new HashSet<OsiService>();
            OsiTariffs = new HashSet<OsiTariff>();
            OsiUsers = new HashSet<OsiUser>();
            ParkingPlaces = new HashSet<ParkingPlace>();
            PaymentOrders = new HashSet<PaymentOrder>();
            Payments = new HashSet<Payment>();
            PlanAccurals = new HashSet<PlanAccural>();
            PromoOperations = new HashSet<PromoOperation>();
            RegistrationHistories = new HashSet<RegistrationHistory>();
            ServiceGroupSaldos = new HashSet<ServiceGroupSaldo>();
            Transactions = new HashSet<Transaction>();
        }

        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Доступен ли ОСИ вообще (это ставим мы)
        /// </summary>
        [Required(ErrorMessage = "Укажите активность")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Запущен в работу (это ставит председатель)
        /// </summary>
        [Required(ErrorMessage = "Укажите готовность кабинета")]
        public bool IsLaunched { get; set; }

        /// <summary>
        /// Id заявки, от которой создан ОСИ
        /// </summary>
        [Required(ErrorMessage = "Укажите Id заявки")]
        public int RegistrationId { get; set; }

        /// <summary>
        /// Шаг визарда
        /// </summary>
        [MaxLength(100)]
        public string WizardStep { get; set; }

        /// <summary>
        /// РКА
        /// </summary>
        [MaxLength(16)]
        public string Rca { get; set; }

        /// <summary>
        /// Ставка МРП в процентах для взносов на капитальный ремонт
        /// </summary>
        [JsonIgnore]
        public decimal BigRepairMrpPercent { get; set; }

        [NotMapped]
        public string HouseStateNameRu => HouseState?.NameRu;

        [NotMapped]
        public string HouseStateNameKz => HouseState?.NameKz;

        public bool TakeComission { get; set; }

        public bool IsInPromo { get; set; }

        public int FreeMonthPromo { get; set; } = 0;

        public bool AccuralsWithDecimals { get; set; }

        /// <summary>
        /// РКА
        /// </summary>
        [MinLength(2, ErrorMessage = "КБе должен состоять из двух символов")]
        [MaxLength(2, ErrorMessage = "КБе должен состоять из двух символов")]
        [Required(ErrorMessage = "Укажите КБе")]
        public string Kbe { get; set; }

        /// <summary>
        /// Дает доступ отобразить на фронте кнопку "Переделать начисления"
        /// </summary>
        public bool CanRemakeAccurals { get; set; }

        [NotMapped]
        public string UnionTypeRu => UnionType?.NameRu;

        [NotMapped]
        public string UnionTypeKz => UnionType?.NameKz;

        [JsonIgnore]
        public virtual Registration Registration { get; set; }

        [JsonIgnore]
        public virtual HouseState HouseState { get; set; }

        [JsonIgnore]
        public virtual UnionType UnionType { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiUser> OsiUsers { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Abonent> Abonents { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiDoc> OsiDocs { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiService> OsiServices { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<PlanAccural> PlanAccurals { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ServiceGroupSaldo> ServiceGroupSaldos { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Transaction> Transactions { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiServiceCompany> OsiServiceCompanies { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiAccount> OsiAccounts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiAccountApplication> OsiAccountApplications { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Act> Acts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Payment> Payments { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Fix> Fixes { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<PaymentOrder> PaymentOrders { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiTariff> OsiTariffs { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Failure> Failures { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ConnectedService> ConnectedServices { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiServiceAmount> OsiServiceAmounts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ParkingPlace> ParkingPlaces { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<PromoOperation> PromoOperations { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<AccountReport> AccountReports { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<RegistrationHistory> RegistrationHistories { get; set; }
    }
}
