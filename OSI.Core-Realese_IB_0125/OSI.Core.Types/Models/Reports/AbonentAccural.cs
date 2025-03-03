using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Reports
{
    public class AbonentAccural
    {
        public string AccuralMethodCode { get; set; }

        public int AbonentId { get; set; }

        public decimal? Square { get; set; }

        public decimal? EffectiveSquare { get; set; }

        public int ServiceId { get; set; }

        public int GroupId { get; set; }

        public string ServiceName { get; set; }

        public decimal? Tarif { get; set; }

        public decimal Debet { get; set; }
        
        public decimal DebetWithoutFixes { get; set; }

        public decimal SumOfFixes { get; set; }
    }
}
