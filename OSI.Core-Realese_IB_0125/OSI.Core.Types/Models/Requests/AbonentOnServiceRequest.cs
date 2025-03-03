using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class AbonentOnServiceRequest
    {
        public int AbonentId { get; set; }
        public bool Checked { get; set; }
        public int ParkingPlaces { get; set; }
    }
}
