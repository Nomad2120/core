using OSI.Core.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class AccountReportRequest
    {
        /// <summary>
        /// Id ОСИ
        /// </summary>
        [RequiredExt(AllowDefault = false, ErrorMessage = "Укажите Id ОСИ")]
        public int OsiId { get; set; }

        /// <summary>
        /// Период
        /// </summary>
        [RequiredExt(AllowDefault = false, ErrorMessage = "Укажите период")]
        public DateTime Period { get; set; }
    }
}
