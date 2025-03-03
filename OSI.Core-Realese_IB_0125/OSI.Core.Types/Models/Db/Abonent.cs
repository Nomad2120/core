using OSI.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class Abonent: AbonentRequest
    {
        public Abonent()
        {
            AbonentHistories = new HashSet<AbonentHistory>();
            Arendators = new HashSet<Arendator>();
            ServiceGroupSaldos = new HashSet<ServiceGroupSaldo>();
            Transactions = new HashSet<Transaction>();
            PastDebts = new HashSet<PastDebt>();
            ConnectedServices = new HashSet<ConnectedService>();
            ParkingPlaces = new HashSet<ParkingPlace>();
            TelegramSubscriptions = new HashSet<TelegramSubscription>();
        }

        [Key]
        public int Id { get; set; }

        public bool IsActive { get; set; }

        [MaxLength(20)]
        public string ErcAccount { get; set; }

        private string areaTypeNameRu = null;
        [NotMapped]
        public string AreaTypeNameRu { get => areaTypeNameRu ?? AreaType?.NameRu; set => areaTypeNameRu = value; }

        private string areaTypeNameKz = null;
        [NotMapped]
        public string AreaTypeNameKz { get => areaTypeNameKz ?? AreaType?.NameKz; set => areaTypeNameKz = value; }

        [NotMapped]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string OsiName { get; set; }

        [NotMapped]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Address { get; set; }

        [NotMapped]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string InvoiceNum { get; set; }

        [JsonIgnore]
        public virtual AreaType AreaType { get; set; }

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<AbonentHistory> AbonentHistories { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Arendator> Arendators { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ServiceGroupSaldo> ServiceGroupSaldos { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Transaction> Transactions { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<PastDebt> PastDebts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ConnectedService> ConnectedServices { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ParkingPlace> ParkingPlaces { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<TelegramSubscription> TelegramSubscriptions { get; set; }
    }
}
