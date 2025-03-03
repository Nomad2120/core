using OSI.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class ModelBase
    {
        public int Id { get; set; }

        public virtual string GetExceptionMessage(Exception ex)
        {
            return $"{ex.GetType().FullName}: {ex.GetFullInfo()}";
        }
    }
}
