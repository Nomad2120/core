using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class ActResponse
    {
        public int Id { get; set; }

        public DateTime CreateDt { get; set; }

        public DateTime? SignDt { get; set; }

        public DateTime ActPeriod { get; set; }

        public string ActNum { get; set; }

        public ActStateCodes StateCode { get; set; }

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        public string OsiName => Osi?.Name;

        public string OsiIdn => Osi?.Idn;

        public string OsiAddress => Osi?.Address;

        public string OsiPhone => Osi?.Phone;

        public DateTime? OsiRegistrationDate => Osi?.Registration?.CreateDt;

        public int? ApartCount => Osi?.ApartCount;

        public int PlanAccuralId { get; set; }

        public decimal Amount { get; set; }

        public decimal Comission { get; set; }

        public decimal Debt { get; set; }

        public decimal Tariff { get; set; }

        public List<ActItem> ActItems { get; set; }

    }
}
