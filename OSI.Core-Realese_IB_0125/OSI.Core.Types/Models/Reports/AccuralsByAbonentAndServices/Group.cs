using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Reports.AccuralsByAbonentAndServices
{
    public class Group
    {
        public string GroupName { get; set; }

        public List<Service> Services { get; set; }
    }
}
