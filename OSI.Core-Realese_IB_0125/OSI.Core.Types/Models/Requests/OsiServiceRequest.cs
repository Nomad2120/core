using OSI.Core.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OSI.Core.Models.Requests
{
    public class OsiServiceRequest
    {
        [Required(ErrorMessage = "Укажите ОСИ")]
        public int OsiId { get; set; }

        [Required(ErrorMessage = "Укажите наименование")]
        public string NameRu { get; set; }

        public string NameKz { get; set; }

        public int ServiceGroupId { get; set; }

        [NotMapped]
        public int AccuralMethodId { get; set; }

        [NotMapped]
        public decimal Amount { get; set; }
    }
}