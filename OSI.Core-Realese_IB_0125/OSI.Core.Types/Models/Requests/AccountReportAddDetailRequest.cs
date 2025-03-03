using OSI.Core.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests
{
    public class AccountReportAddDetailRequest
    {
        /// <summary>
        /// Id записи об операции
        /// </summary>
        [RequiredExt(AllowDefault = false, ErrorMessage = "Укажите Id записи об операции")]
        public int ItemId { get; set; }

        /// <summary>
        /// Сумма
        /// </summary>
        [RequiredExt(AllowDefault = false, ErrorMessage = "Укажите сумму")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Комментарий
        /// </summary>
        [RequiredExt(AllowEmptyStrings = false, ErrorMessage = "Укажите комментарий")]
        [MaxLength(500, ErrorMessage = "Длина комментария не должна превышать 500 символов")]
        public string Comment { get; set; }
    }
}
