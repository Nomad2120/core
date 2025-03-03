using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class AccountReportPublishRequest
    {
        /// <summary>
        /// Сумма задолженности по обязательным ежемесячным взносам собственников квартир, нежилых помещений на управление объекта кондоминиума и содержание общего имущества объекта кондоминиума
        /// </summary>
        public decimal MaintenanceAmount { get; set; }

        /// <summary>
        /// Сумма задолженности по обязательным ежемесячным взносам собственников квартир, нежилых помещений для накоплений сумм на капитальный ремонт
        /// </summary>
        public decimal SavingsAmount { get; set; }

        /// <summary>
        /// Сумма задолженности по обязательным ежемесячным взносам собственников парковочных мест, кладовок за содержание парковочного места, кладовки
        /// </summary>
        public decimal ParkingAmount { get; set; }
    }
}
