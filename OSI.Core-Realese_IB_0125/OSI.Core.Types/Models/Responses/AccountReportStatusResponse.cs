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
    /// <summary>
    /// Статус отчета председателя перед жильцами
    /// </summary>
    public class AccountReportStatusResponse
    {
        /// <summary>
        /// Id отчета
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Состояние
        /// </summary>
        public AccountReportStateCodes State { get; set; }

        /// <summary>
        /// Период
        /// </summary>
        public DateTime Period { get; set; }

        /// <summary>
        /// Списки по счетам
        /// </summary>
        public IEnumerable<AccountReportListStatusResponse> Lists { get; set; }
    }

    public class AccountReportListStatusResponse
    {
        /// <summary>
        /// Id списка
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Счет
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// Тип счета
        /// </summary>
        [JsonPropertyName("accountType")]
        public AccountTypeCodes AccountTypeCode { get; set; }

        public string AccountTypeNameRu => AccountType?.NameRu;

        public string AccountTypeNameKz => AccountType?.NameKz;

        /// <summary>
        /// БИК банка
        /// </summary>
        public string Bic { get; set; }

        public string BankName => Bank?.Name;

        public string BankStatementVideoUrl => Bank?.StatementVideoUrl;

        /// <summary>
        /// Прикреплена выписка или нет
        /// </summary>
        public bool IsFilled { get; set; }

        [JsonIgnore]
        public virtual AccountType AccountType { get; set; }

        [JsonIgnore]
        public virtual Bank Bank { get; set; }
    }
}
