using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class Role : ModelBase
    {
        public Role()
        {
            UserRoles = new HashSet<UserRole>();
        }

        public override string GetExceptionMessage(Exception ex)
        {
            string message = base.GetExceptionMessage(ex);
            if (ex is DbUpdateException)
            {
                if (ex.InnerException.Message.IndexOf("duplicate key") > -1)
                {
                    if (ex.InnerException.Message.IndexOf("idx_code_uniq") > -1)
                        message = "Такая роль уже существует";
                }
            }
            return message;
        }

        private string _code;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите код роли")]
        [MaxLength(30)]
        public string Code
        {
            get
            {
                if (string.IsNullOrEmpty(_code)) return "";
                return _code.ToUpper();
            }
            set
            {
                _code = value;
            }
        }

        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string NameKz { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<UserRole> UserRoles { get; set; }
    }
}
