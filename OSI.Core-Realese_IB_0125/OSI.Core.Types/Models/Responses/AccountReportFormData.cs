using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class AccountReportFormData
    {
        public DateTime Period { get; set; }
        public string OsiName { get; set; }
        public string OsiAddress { get; set; }
        public string Signer { get; set; }
        public string UnionTypeRu => UnionType?.NameRu;
        public string UnionTypeKz => UnionType?.NameKz;
        [JsonIgnore]
        public UnionType UnionType { get; set; }
        public IEnumerable<AccountReportCategoryFormData> Categories { get; set; }
    }

    public class AccountReportCategoryFormData
    {
        public string Number { get; set; }
        public string NameRu { get; set; }
        public string NameKz { get; set; }
        public decimal Amount { get; set; }
    }
}
