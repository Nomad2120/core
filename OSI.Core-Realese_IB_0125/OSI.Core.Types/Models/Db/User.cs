using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Requests;
using OSI.Core.Validators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Db
{
    public class User : UserRequest
    {
        public User()
        {
            OsiUsers = new HashSet<OsiUser>();
            UserRoles = new HashSet<UserRole>();
            Registrations = new HashSet<Registration>();
            Payments = new HashSet<Payment>();
        }

        //public override string GetExceptionMessage(Exception ex)
        //{
        //    string message = base.GetExceptionMessage(ex);
        //    if (ex is DbUpdateException)
        //    {
        //        if (ex.InnerException.Message.IndexOf("duplicate key") > -1)
        //        {
        //            if (ex.InnerException.Message.IndexOf("users_idx_code") > -1)
        //                message = "Такой пользователь уже существует";
        //        }
        //    }
        //    return message;
        //}

        [Key]
        public int Id { get; set; }

        private string _code;

        /// <summary>
        /// Код пользователя, у председателей он такой же как номер телефона
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "Укажите код пользователя")]
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
        [JsonIgnore]
        public string Password { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<OsiUser> OsiUsers { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<UserRole> UserRoles { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Registration> Registrations { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Payment> Payments { get; set; }

        [DataMember(EmitDefaultValue = false)]
        [JsonIgnore]
        public virtual ICollection<Fix> Fixes { get; set; }
    }
}
