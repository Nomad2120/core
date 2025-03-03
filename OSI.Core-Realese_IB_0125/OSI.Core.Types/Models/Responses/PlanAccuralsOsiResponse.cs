using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class PlanAccuralsOsiResponse
    {
        public PlanAccuralsOsiResponse()
        {
            PlanAccurals = new List<PlanAccural>();
        }
        public Osi Osi { get; set; }
        public List<PlanAccural> PlanAccurals { get; set; }
    }
}
