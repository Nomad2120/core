using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class OsiServiceResponse
    {
        public int Id { get; set; }

        public string NameRu { get; set; }

        public string NameKz { get; set; }

        //public string ServiceGroupNameRu { get; set; }

        //public string ServiceGroupNameKz { get; set; }

        public int ServiceGroupId { get; set; }

        public int AccuralMethodId { get; set; }

        public decimal Amount { get; set; }

        public bool IsOsiBilling { get; set; }

        public bool IsActive { get; set; }

        public int CountAllAbonents { get; set; }

        public int CountActiveAbonents { get; set; }

        //public List<AbonentOnServiceResponse> Abonents { get; set; }
    }
}
