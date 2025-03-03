using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class AccountReportCategoryResponse
    {
        public int Id { get; set; }

        public string Number { get; set; }

        public string NameRu { get; set; }

        public string NameKz { get; set; }

        [JsonPropertyName("accountType")]
        public AccountTypeCodes AccountTypeCode { get; set; }

        public string AccountTypeNameRu => AccountType?.NameRu;

        public string AccountTypeNameKz => AccountType?.NameKz;

        [JsonPropertyName("operationType")]
        public OperationTypeCodes OperationTypeCode { get; set; }

        public string OperationTypeNameRu => OperationType?.NameRu;

        public string OperationTypeNameKz => OperationType?.NameKz;

        [JsonIgnore]
        public virtual AccountType AccountType { get; set; }

        [JsonIgnore]
        public virtual OperationType OperationType { get; set; }
    }
}
