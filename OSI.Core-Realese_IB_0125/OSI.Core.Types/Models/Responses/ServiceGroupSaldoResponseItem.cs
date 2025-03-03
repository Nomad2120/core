using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class ServiceGroupSaldoResponseItem
    {
        public int Id { get; set; }

        public string AbonentName { get; set; }

        // OSI-142 добавление типа помещения 
        public AreaTypeCodes AreaTypeCode { get; set; }

        public string Flat { get; set; }

        public decimal Saldo { get; set; }
    }
}
