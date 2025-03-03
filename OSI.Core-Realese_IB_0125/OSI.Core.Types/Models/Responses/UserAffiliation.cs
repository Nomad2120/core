using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class UserAffiliationAbonent
    {
        public int AbonentId { get; set; }

        public string AbonentName { get; set; }

        public string Flat { get; set; }
    }

    public class UserAffiliation
    {
        public int OsiId { get; set; }

        public string OsiName { get; set; }

        public string Address { get; set; }

        public List<UserAffiliationAbonent> Abonents { get; set; }
    }
}
