using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class ServiceGroupSaldoRequest
    {
        /// <summary>
        /// Абонент
        /// </summary>
        public int AbonentId { get; set; }

        /// <summary>
        /// Группа
        /// </summary>
        public int GroupId { get; set; }
        
        /// <summary>
        /// ОСИ id
        /// </summary>
        public int OsiId { get; set; }

        /// <summary>
        /// Сальдо абонента по услуге
        /// </summary>
        public decimal Saldo { get; set; }
    }
}
