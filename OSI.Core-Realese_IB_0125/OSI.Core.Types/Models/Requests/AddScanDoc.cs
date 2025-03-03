using OSI.Core.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace OSI.Core.Models.Requests
{
    public class AddScanDoc
    {
        /// <summary>
        /// Тип документа
        /// </summary>
        [Required]
        public string DocTypeCode { get; set; }

        /// <summary>
        /// Данные файла в base64
        /// </summary>
        [Required]
        public byte[] Data { get; set; }

        /// <summary>
        /// Расширение файла
        /// </summary>
        public string Extension { get; set; }
    }
}