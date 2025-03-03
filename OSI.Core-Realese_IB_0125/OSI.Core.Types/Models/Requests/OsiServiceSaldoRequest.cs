using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class OsiServiceSaldoRequest
    {
        /// <summary>
        /// Абонент
        /// </summary>
        public int AbonentId { get; set; }

        /// <summary>
        /// Услуга ОСИ
        /// </summary>
        public int OsiServiceId { get; set; }

        /// <summary>
        /// Сальдо абонента по услуге
        /// </summary>
        public decimal Saldo { get; set; }
    }
}
