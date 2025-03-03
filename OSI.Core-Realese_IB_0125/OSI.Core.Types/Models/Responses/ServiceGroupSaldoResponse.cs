using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class ServiceGroupSaldoResponse
    {
        public int GroupId { get; set; }

        public string GroupNameRu { get; set; }

        public string GroupNameKz { get; set; }

        public IEnumerable<ServiceGroupSaldoResponseItem> Items { get; set; }
    }
}
