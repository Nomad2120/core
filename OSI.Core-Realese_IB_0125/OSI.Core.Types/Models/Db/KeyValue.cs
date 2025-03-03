using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class KeyValue
    {
        [Key]
        [MinLength(1)]
        public string Key { get; set; }

        public string Value { get; set; }
    }
}
