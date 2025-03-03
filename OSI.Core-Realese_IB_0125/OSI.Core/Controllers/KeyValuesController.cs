using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSI.Core.Auth;
using OSI.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Хранилище ключ-значение
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class KeyValuesController : ControllerBase
    {
        private readonly IKeyValueSvc keyValueSvc;

        public KeyValuesController(IKeyValueSvc keyValueSvc)
        {
            this.keyValueSvc = keyValueSvc;
        }

        /// <summary>
        /// Получить данные по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns></returns>
        [HttpGet("{key}")]
        public Task<string> Get(string key) => keyValueSvc.Get(key);

        /// <summary>
        /// Получить данные по ключам
        /// </summary>
        /// <param name="keys">Ключи</param>
        /// <returns></returns>
        [HttpGet]
        public Task<IReadOnlyDictionary<string, string>> Get([FromQuery(Name = "key")] IEnumerable<string> keys) => keyValueSvc.Get(keys);

        /// <summary>
        /// Получить данные по ключам
        /// </summary>
        /// <param name="keys">Ключи</param>
        /// <returns></returns>
        [HttpPost]
        public Task<IReadOnlyDictionary<string, string>> GetByBody([FromBody] IEnumerable<string> keys) => keyValueSvc.Get(keys);
    }
}
