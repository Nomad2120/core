using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OSI.Core.Models.Db
{
    [Table("registration_history")]
    public class RegistrationHistory
    {
        [Key]
        public int Id { get; set; }

        public int OsiId { get; set; }

        public int RegistrationId { get; set; }

        public DateTime ApproveDate { get; set; }

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        [JsonIgnore]
        public virtual Registration Registration { get; set; }
    }
}
