using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class BaseRate : ModelBase
    {
        public DateTime Period { get; set; }

        public decimal Value{ get; set; }
    }
}
