using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class UserContactResponse
    {
        /// <summary>
        /// Пользователь поделился контактом с ботом
        /// </summary>
        public bool IsContacted { get; set; }

        /// <summary>
        /// У пользователя есть постоянный логин
        /// </summary>
        public bool IsRegistered { get; set; }

        /// <summary>
        /// У пользователя есть постоянный пароль
        /// </summary>
        public bool HasPassword { get; set; }

        /// <summary>
        /// Ссылка на бота
        /// </summary>
        public string BotUrl { get; set; }

        [JsonIgnore]
        public long ChatId { get; set; }
    }
}
