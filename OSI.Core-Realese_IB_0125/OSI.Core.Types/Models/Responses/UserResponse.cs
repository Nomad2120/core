using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class UserResponse
    {
        /// <summary>
        /// Id пользователя
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Логин
        /// </summary>
        public string Code { get; set; }
        
        /// <summary>
        /// ФИО
        /// </summary>
        public string FIO { get; set; }

        /// <summary>
        /// ИИН
        /// </summary>
        public string IIN { get; set; }

        /// <summary>
        /// Телефон
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Емаил
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Роли пользователя
        /// </summary>
        public IEnumerable<RoleResponse> Roles { get; set; }
    }
}
