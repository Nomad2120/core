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
using OSI.Core.Models.Enums;

namespace OSI.Core.Models.Db
{
    public class OsiService: OsiServiceRequest
    {
        public OsiService()
        {
            Transactions = new HashSet<Transaction>();
            ConnectedServices = new HashSet<ConnectedService>();
            OsiServiceAmounts = new HashSet<OsiServiceAmount>();
            ParkingPlaces = new HashSet<ParkingPlace>();
        }

        [Key]
        public int Id { get; set; }        

        [Required(ErrorMessage = "Укажите признак услуги Osi billing")]
        public bool IsOsibilling { get; set; }

        [Required(ErrorMessage = "Укажите активность")]
        public bool IsActive { get; set; }

        [NotMapped]
        public string ServiceGroupNameRu => ServiceGroup?.NameRu;

        [NotMapped]
        public string ServiceGroupNameKz => ServiceGroup?.NameKz;

        [NotMapped]
        public bool CopyToNextPeriod => ServiceGroup?.CopyToNextPeriod ?? false;

        //-------------------------- virtuals
        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        [JsonIgnore]
        public virtual ServiceGroup ServiceGroup { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Transaction> Transactions { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ConnectedService> ConnectedServices { get; set; }
        
        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiServiceAmount> OsiServiceAmounts { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<ParkingPlace> ParkingPlaces { get; set; }
    }
}
