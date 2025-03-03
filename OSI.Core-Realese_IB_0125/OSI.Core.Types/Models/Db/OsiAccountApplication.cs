using IbanNet.DataAnnotations;
using OSI.Core.Models.Enums;
using OSI.Core.Validators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using OSI.Core.Models.Requests;
using System.Runtime.Serialization;

namespace OSI.Core.Models.Db
{
    public class OsiAccountApplication : OsiAccountApplicationRequest
    {
        [Key]
        public int Id { get; set; }

        public DateTime CreateDt { get; set; }

        public string State { get; set; } = "CREATED"; //CREATED, PENDING, APPROVED, REJECTED

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string RejectReason { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [Column("old_bic")]
        public string OldBankBic { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string OldAccount { get; set; }

        [NotMapped]
        public string ApplicationTypeText => ApplicationType switch
        {
            "ADD" => "Добавление",
            "UPDATE" => "Изменение",
            //"DELETE" => "Удаление",
            _ => "Неизвестно"
        };

        [NotMapped]
        public string StateText => State switch
        {
            "CREATED" => "Создана",
            "PENDING" => "Ожидает рассмотрения",
            "APPROVED" => "Обработана",
            "REJECTED" => "Отклонена",
            _ => "Неизвестно"
        };

        [JsonIgnore]
        [NotMapped]
        public string OsiName => Osi?.Name;

        [NotMapped]
        public string BankName => Bank?.Name;

        [NotMapped]
        public string AccountTypeNameRu => AccountType?.NameRu;

        [NotMapped]
        public string AccountTypeNameKz => AccountType?.NameKz;

        [JsonIgnore]
        public virtual Osi Osi { get; set; }

        [JsonIgnore]
        public virtual OsiAccount OsiAccount { get; set; }

        [JsonIgnore]
        public virtual Bank Bank { get; set; }

        [JsonIgnore]
        public virtual AccountType AccountType { get; set; }

        [JsonIgnore]
        public virtual ServiceGroup ServiceGroup { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiAccountApplicationDoc> OsiAccountApplicationDocs { get; set; }
    }
}
