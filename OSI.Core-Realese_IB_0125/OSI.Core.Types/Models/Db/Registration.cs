using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using OSI.Core.Models.Enums;
using OSI.Core.Models.Requests;

namespace OSI.Core.Models.Db
{
    public class Registration: RegistrationRequest
    {
        public Registration()
        {
            RegistrationDocs = new HashSet<RegistrationDoc>();
            Osies = new HashSet<Osi>();
            RegistrationAccounts = new HashSet<RegistrationAccount>();
            RegistrationHistories = new HashSet<RegistrationHistory>();
        }

        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Состояние 
        /// </summary>
        [Column("state")]
        [Required(ErrorMessage = "Укажите состояние")]
        public RegistrationStateCodes StateCode { get; set; }

        [NotMapped]
        public string StateName => State?.Name;

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTime CreateDt { get; set; }

        // OSI-122
        /// <summary>
        /// Тариф
        /// </summary>
        public decimal Tariff { get; set; }

        //OSI-194
        /// <summary>
        /// Дата подписания заявки
        /// </summary>
        public DateTime? SignDt { get; set; }

        /// <summary>
        /// Шаг визарда
        /// </summary>
        [MaxLength(20)]
        public string WizardStep { get; set; }

        /// <summary>
        /// Разновидность заявки
        /// </summary>
        public string RegistrationKind { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public string RegistrationKindText => RegistrationKind switch
        {
            "INITIAL" => "Регистрация ОСИ",
            "CHANGE_CHAIRMAN" => "Смена председателя",
            "CHANGE_UNION_TYPE" => "Смена формы управления",
            _ => RegistrationKind,
        };

        public string ReqTypeCode { get; set; }

        public string RejectReason { get; set; }

        [JsonIgnore]
        public virtual ReqType ReqType { get; set; }

        [JsonIgnore]
        public virtual RegistrationState State { get; set; }

        [JsonIgnore]
        public virtual User User { get; set; }

        [JsonIgnore]
        public virtual UnionType UnionType { get; set; }

        [NotMapped]
        public string UnionTypeRu => UnionType?.NameRu;

        [NotMapped]
        public string UnionTypeKz => UnionType?.NameKz;

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<RegistrationDoc> RegistrationDocs { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Osi> Osies { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<RegistrationAccount> RegistrationAccounts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<RegistrationHistory> RegistrationHistories { get; set; }
    }
}
