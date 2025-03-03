using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Reports.AccuralsByAbonentAndServices
{
    public class Service
    {
        public string ServiceName { get; set; }
        public decimal Accural { get; set; }
        public decimal Fix { get; set; }
        public decimal Total { get; set; }
    }
}
