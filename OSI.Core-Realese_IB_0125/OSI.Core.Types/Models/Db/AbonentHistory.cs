using OSI.Core.Models.Enums;
using OSI.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    /// <summary>
    /// Данные абонентов на момент создания начислений
    /// </summary>
    [Table("abonent_histories")]
    public class AbonentHistory: AbonentRequest
    {
        [Key]
        public int Id { get; set; }

        public DateTime Dt { get; set; }

        public int AbonentId { get; set; }

        public bool IsActive { get; set; }

        public virtual Abonent Abonent { get; set; }
    }
}
