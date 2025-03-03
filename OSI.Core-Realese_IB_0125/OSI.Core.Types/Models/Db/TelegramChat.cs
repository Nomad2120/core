using System.ComponentModel.DataAnnotations;

namespace OSI.Core.Models.Db
{
    public class TelegramChat : ModelBase
    {
        [Required]
        public long ChatId { get; set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(20)]
        public string Phone { get; set; }

        [MaxLength(200)]
        public string FIO { get; set; }
    }
}