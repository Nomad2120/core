using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class AccountReportDoc : ModelBase
    {
        [JsonIgnore]
        public int AccountReportId { get; set; }

        public string DocTypeCode { get; set; }

        private string docTypeNameRu = null;
        [NotMapped]
        public string DocTypeNameRu { get => docTypeNameRu ?? DocType?.NameRu; set => docTypeNameRu = value; }

        private string docTypeNameKz = null;
        [NotMapped]
        public string DocTypeNameKz { get => docTypeNameKz ?? DocType?.NameKz; set => docTypeNameKz = value; }

        [JsonIgnore]
        public int ScanId { get; set; }

        [JsonIgnore]
        public virtual DocType DocType { get; set; }

        [JsonIgnore]
        public virtual AccountReport AccountReport { get; set; }

        public virtual Scan Scan { get; set; }
    }
}
