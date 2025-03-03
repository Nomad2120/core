using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    [Table("system_information")]
    public class SystemInformation
    {
        [Key]
        public string Code { get; set; }

        public DateTime? DateValue { get; set; }

        public int? IntValue { get; set; }

        public string StrValue { get; set; }
    }
}
