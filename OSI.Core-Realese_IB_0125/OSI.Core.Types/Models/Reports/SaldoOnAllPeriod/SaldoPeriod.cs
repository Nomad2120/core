using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Reports.SaldoOnAllPeriod
{
    public class SaldoPeriod
    {
        [JsonIgnore]
        public DateTime Period { get; set; }

        [JsonPropertyName("period")]
        public string PeriodDescription { get; set; }

        public List<SaldoPeriodItem> Services { get; set; }
    }
}
