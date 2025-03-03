using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class Tariff : ModelBase
    {
        public int AtsId { get; set; }

        public string AtsFullPath { get; set; }

        public decimal Value { get; set; }

        public DateTime Date { get; set; }
    }
}
