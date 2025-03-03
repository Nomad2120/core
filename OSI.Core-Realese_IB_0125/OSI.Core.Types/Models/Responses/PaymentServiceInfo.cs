using OSI.Core.Models.Requests;
using OSI.Core.Models.Responses;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class PaymentServiceInfo : PaymentService
    {
        public PaymentServiceInfo(EndSaldoService endSaldoService)
        {
            ServiceId = endSaldoService.ServiceId;
            ServiceName = endSaldoService.ServiceName;
            Sum = endSaldoService.Debt;
        }

        /// <summary>
        /// Наименование услуги
        /// </summary>
        public string ServiceName { get; set; }
    }
}
