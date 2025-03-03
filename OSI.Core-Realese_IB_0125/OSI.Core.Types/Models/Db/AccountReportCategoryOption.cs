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
    public class AccountReportCategoryOption : ModelBase
    {
        public int CategoryId { get; set; }

        /// <summary>
        /// Тип счета
        /// </summary>
        [JsonPropertyName("accountType")]
        public AccountTypeCodes AccountTypeCode { get; set; }

        [NotMapped]
        public string AccountTypeNameRu => AccountType?.NameRu;

        [NotMapped]
        public string AccountTypeNameKz => AccountType?.NameKz;

        /// <summary>
        /// Тип операции
        /// </summary>
        [JsonPropertyName("operationType")]
        public OperationTypeCodes OperationTypeCode { get; set; }

        [NotMapped]
        public string OperationTypeNameRu => OperationType?.NameRu;

        [NotMapped]
        public string OperationTypeNameKz => OperationType?.NameKz;

        [JsonIgnore]
        public virtual AccountReportCategory Category { get; set; }

        [JsonIgnore]
        public virtual AccountType AccountType { get; set; }

        [JsonIgnore]
        public virtual OperationType OperationType { get; set; }
    }
}
