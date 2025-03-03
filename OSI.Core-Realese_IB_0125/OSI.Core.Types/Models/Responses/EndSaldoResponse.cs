using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class EndSaldoResponse
    {
        /// <summary>
        /// Группы
        /// </summary>
        public List<EndSaldoService> Services { get; set; }

        /// <summary>
        /// Итоговая задолженность
        /// </summary>
        public decimal TotalDebt { get; set; }
    }
}
