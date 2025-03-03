using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using OSI.Core.Models.Requests;

namespace OSI.Core.Models.Db
{
    public class PlanAccural
    {
        public PlanAccural()
        {
            Acts = new HashSet<Act>();
            Transactions = new HashSet<Transaction>();
        }

        [Key]
        public int Id { get; set; }

        public int OsiId { get; set; }

        public DateTime BeginDate { get; set; }

        public bool AccuralCompleted { get; set; }

        public bool UssikingIncluded { get; set; }

        public int ApartCount { get; set; }

        public decimal Tariff { get; set; }

        public DateTime? AccuralDate { get; set; }

        public int AccuralJobAtDay { get; set; }

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Transaction> Transactions { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Act> Acts { get; set; }
    }
}
