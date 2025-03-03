using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class ConnectedService
    {
        [Key]
        public int Id { get; set; }

        public int OsiId { get; set; }

        public int AbonentId { get; set; }

        public int OsiServiceId { get; set; }

        public bool IsActive { get; set; }

        public DateTime Dt { get; set; }

        public string Note { get; set; }

        //-------------------------- virtuals

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        [JsonIgnore]
        public virtual Abonent Abonent { get; set; }

        [JsonIgnore]
        public virtual OsiService OsiService { get; set; }
    }
}
