using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class AbonentOnServiceResponse : Abonent
    {
        public AbonentOnServiceResponse(Abonent abonent)
        {
            Id = abonent.Id;
            OsiId = abonent.OsiId;
            Name = abonent.Name;
            Flat = abonent.Flat;
            Idn = abonent.Idn;
            AreaTypeCode = abonent.AreaTypeCode;
            Phone = abonent.Phone;
            Floor = abonent.Floor;
            Square = abonent.Square;
            LivingJur = abonent.LivingJur;
            LivingFact = abonent.LivingFact;
            AreaType = abonent.AreaType;
            Osi = abonent.Osi;
            Owner = abonent.Owner;
            ErcAccount = abonent.ErcAccount;
        }

        public bool Checked { get; set; }

        public new int ParkingPlaces { get; set; }
    }
}
