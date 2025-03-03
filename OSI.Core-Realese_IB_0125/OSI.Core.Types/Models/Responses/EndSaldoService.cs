using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class EndSaldoService
    {
        /// <summary>
        /// Id группы
        /// </summary>
        public int ServiceId { get; set; }

        /// <summary>
        /// Наименование группы
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Задолженность по группе
        /// </summary>
        public decimal Debt { get; set; }
    }
}
