using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class Act
    {
        public Act()
        {
            ActDocs = new HashSet<ActDoc>();
            ActItems = new HashSet<ActItem>();
            ActOperations = new HashSet<ActOperation>();
            PromoOperations = new HashSet<PromoOperation>();
        }

        [Key]
        public int Id { get; set; }

        public DateTime CreateDt { get; set; }
        
        public DateTime? SignDt { get; set; }

        public DateTime ActPeriod { get; set; }

        [Required(ErrorMessage = "Укажите номер акта")]
        [MaxLength(30)]
        public string ActNum { get; set; }

        [Column("state")]
        [Required(ErrorMessage = "Укажите состояние")]
        public ActStateCodes StateCode { get; set; }

        [JsonIgnore]
        public virtual ActState State { get; set; }

        [NotMapped]
        public string StateName => State?.Name;

        public int OsiId { get; set; }

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        [NotMapped]
        public string OsiName => Osi?.Name;

        [NotMapped]
        public string OsiIdn => Osi?.Idn;

        [NotMapped]
        public string OsiAddress => Osi?.Address;

        [NotMapped]
        public string OsiPhone => Osi?.Phone;

        [NotMapped]
        public DateTime? OsiRegistrationDate => Osi?.Registration?.CreateDt;

        [NotMapped]
        public int? ApartCount => Osi?.ApartCount;

        public int PlanAccuralId { get; set; }

        [JsonIgnore]
        public virtual PlanAccural PlanAccural { get; set; }

        public decimal Amount { get; set; }

        public decimal Comission { get; set; }

        public decimal Debt { get; set; }

        public decimal Tariff { get; set; }

        [NotMapped]
        public string ActDateStr => ActPeriod.Month switch
        {
            1 => "январь",
            2 => "февраль",
            3 => "март",
            4 => "апрель",
            5 => "май",
            6 => "июнь",
            7 => "июль",
            8 => "август",
            9 => "сентябрь",
            10 => "октябрь",
            11 => "ноябрь",
            12 => "декабрь",
            _ => ""
        } + " " + ActPeriod.Year + " г.";

        public string EsfNum { get; set; }

        public string EsfError { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ActDoc> ActDocs { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ActOperation> ActOperations { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ActItem> ActItems { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<PromoOperation> PromoOperations { get; set; }
    }
}
