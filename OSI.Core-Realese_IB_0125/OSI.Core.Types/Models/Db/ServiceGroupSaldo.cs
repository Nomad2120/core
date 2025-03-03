using OSI.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    [Table("service_group_saldo")]
    public class ServiceGroupSaldo : ServiceGroupSaldoRequest
    {
        [Key]
        public int Id { get; set; }

        public int TransactionId { get; set; }

        private string abonentName;
        [NotMapped]
        public string AbonentName { get => abonentName ?? Abonent?.Name; set => abonentName = value; }

        private string abonentFlat;
        [NotMapped]
        public string AbonentFlat { get => abonentFlat ?? Abonent?.Flat; set => abonentFlat = value; }

        [NotMapped]
        public string GroupNameRu => Group?.NameRu;

        [NotMapped]
        public string GroupNameKz => Group?.NameKz;

        [JsonIgnore]
        public virtual Abonent Abonent { get; set; }

        [JsonIgnore]
        public virtual ServiceGroup Group { get; set; }

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        [JsonIgnore]
        public virtual Transaction Transaction { get; set; }
    }
}
