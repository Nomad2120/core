﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class OsiTariff
    {
        [Key]
        public int Id { get; set; }

        public DateTime Dt { get; set; }

        public decimal Value { get; set; }

        public int OsiId { get; set; }

        [JsonIgnore]
        public virtual Osi Osi { get; set; }
    }
}
