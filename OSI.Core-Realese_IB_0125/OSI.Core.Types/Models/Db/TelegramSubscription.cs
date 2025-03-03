using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class TelegramSubscription : ModelBase
    {
        [Required]
        public long ChatId { get; set; }

        [Required]
        public int AbonentId { get; set; }

        public DateTime? Dt { get; set; }

        public virtual Abonent Abonent { get; set; }
    }
}
