using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using OSI.Core.Services;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Заявки на добавление/изменение счетов
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OsiAccountApplicationsController : ControllerBase
    {
        private readonly IOsiAccountApplicationSvc osiAccountApplicationSvc;

        public OsiAccountApplicationsController(IOsiAccountApplicationSvc osiAccountApplicationSvc)
        {
            this.osiAccountApplicationSvc = osiAccountApplicationSvc;
        }

        /// <summary>
        /// Получить заявку по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор заявки</param>
        /// <returns></returns>
        [Authorize(Exclude = Roles.ABONENT)]
        [HttpGet("{id}")]
        [UserHasAccessFilter(typeof(OsiAccountApplication), nameof(id))]
        public Task<ApiResponse<OsiAccountApplication>> GetOsiAccountApplicationById(int id)
            => ApiResponse.CreateEx(() => osiAccountApplicationSvc.GetOsiAccountApplicationById(id));

        /// <summary>
        /// Получить документы заявки по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор заявки</param>
        /// <returns></returns>
        [Authorize(Exclude = Roles.ABONENT)]
        [HttpGet("{id}/docs")]
        [UserHasAccessFilter(typeof(OsiAccountApplication), nameof(id))]
        public Task<ApiResponse<IEnumerable<OsiAccountApplicationDoc>>> GetOsiAccountApplicationDocs(int id)
            => ApiResponse.CreateEx(() => osiAccountApplicationSvc.GetOsiAccountApplicationDocs(id));

        /// <summary>
        /// Проверить есть ли активная заявка
        /// </summary>
        /// <param name="request">Запрос на проверку заявки</param>
        /// <returns></returns>
        [Authorize(Exclude = Roles.ABONENT)]
        [HttpPost("check")]
        [UserHasAccessFilter(typeof(Osi), nameof(request), nameof(request.OsiId))]
        public Task<ApiResponse> CheckActiveApplication(OsiAccountApplicationCheckRequest request)
            => ApiResponse.CreateEx(() => osiAccountApplicationSvc.CheckActiveApplication(request));

        /// <summary>
        /// Подать заявку
        /// </summary>
        /// <param name="request">Запрос для подачи заявки</param>
        /// <returns></returns>
        [Authorize(Exclude = Roles.ABONENT)]
        [HttpPost]
        [UserHasAccessFilter(typeof(Osi), nameof(request), nameof(request.OsiId))]
        public Task<ApiResponse<OsiAccountApplication>> CreateOsiAccountApplication(OsiAccountApplicationRequest request)
            => ApiResponse.CreateEx(() => osiAccountApplicationSvc.CreateOsiAccountApplication(request));

        /// <summary>
        /// Прикрепить документ
        /// </summary>
        /// <param name="id">Идентификатор заявки</param>
        /// <param name="request">Сканированный документ, где data это byte[]</param>
        /// <returns></returns>
        [Authorize(Exclude = Roles.ABONENT)]
        [HttpPost("{id}/docs")]
        [UserHasAccessFilter(typeof(OsiAccountApplication), nameof(id))]
        public Task<ApiResponse<OsiAccountApplicationDoc>> AddDoc(int id, [FromBody] AddScanDoc request)
            => ApiResponse.CreateEx(() => osiAccountApplicationSvc.AddDoc(id, request));
    }
}
