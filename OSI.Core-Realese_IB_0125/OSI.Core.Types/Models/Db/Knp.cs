using OSI.Core.Models.Enums;
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
    [Table("knps")]
    public class Knp
    {
        [Key]
        public string Code { get; set; }

        [MaxLength(200)]
        public string NameRu { get; set; }

        [MaxLength(200)]
        public string NameKz { get; set; }
    }
}
