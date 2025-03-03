using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Reports
{
    public class OSVAbonent
    {
        public int AbonentId { get; set; }
        
        public string AbonentName { get; set; }

        public bool IsActive { get; set; }

        [JsonIgnore]
        public string ErcAccount { get; set; }

        // OSI-142 добавление типа помещения 
        public AreaTypeCodes AreaTypeCode { get; set; }
        
        public string Flat { get; set; }

        // OSI-158 Отражение признака владельца помещения
        public string Owner { get; set; }

        [JsonIgnore]
        public Dictionary<string, OSVSaldo> ServicesSaldo { get; set; }
        
        public List<OSVSaldo> Services => ServicesSaldo.OrderBy(s => s.Value.ServiceGroupId).Select(kvp =>
        {
            kvp.Value.ServiceName = kvp.Key;
            return kvp.Value;
        }).ToList();
    }
}
