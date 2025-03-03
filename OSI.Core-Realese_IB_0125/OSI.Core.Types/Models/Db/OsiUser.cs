using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class OsiUser
    {   
        public int Id { get; set; }

        public int UserId { get; set; }
        
        public int OsiId { get; set; }

        public DateTime? CreateDt { get; set; }

        public virtual Osi Osi { get; set; }

        public virtual User User { get; set; }
    }
}
