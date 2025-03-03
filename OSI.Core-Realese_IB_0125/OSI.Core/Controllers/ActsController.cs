using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using OSI.Core.Models.Responses;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Акты выполненных работ
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ActsController : ControllerBase
    {
        private readonly IActSvc actSvc;

        public ActsController(IActSvc actSvc)
        {
            this.actSvc = actSvc;
        }

        /// <summary>
        /// Получить данные акта
        /// </summary>
        /// <param name="id">Id акта</param>
        [HttpGet("{id:int}")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Act), nameof(id))]
        public async Task<ApiResponse<ActResponse>> GetActResponseById(int id)
        {
            var apiResponse = new ApiResponse<ActResponse>();
            try
            {
                var actResponse = await actSvc.GetActResponseById(id);
                apiResponse.Result = actResponse;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Добавление сканированного документа к акту
        /// </summary>
        /// <param name="id">Id акта</param>
        /// <param name="request">Сканированный документ, где data это byte[]</param>
        /// <response code="200">Данные добавленного документа</response>
        [HttpPost("{id:int}/docs")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Act), nameof(id))]
        public async Task<ApiResponse<ActDoc>> AddActDoc(int id, [FromBody] AddScanDoc request)
        {
            var apiResponse = new ApiResponse<ActDoc>();
            try
            {
                var actDoc = await actSvc.AddActDoc(id, request);
                apiResponse.Result = actDoc;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }

            return apiResponse;
        }

        /// <summary>
        /// Получить сканированные документы по акту
        /// </summary>
        /// <param name="id">Id акта</param>
        /// <returns></returns>
        [HttpGet("{id:int}/docs")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Act), nameof(id))]
        public async Task<ApiResponse<IEnumerable<ActDoc>>> GetActsDocs(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<ActDoc>>();
            try
            {
                var actDocs = await actSvc.GetActsDocs(id);
                apiResponse.Result = actDocs;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }

            return apiResponse;
        }

        /// <summary>
        /// Удалить сканированный документ у акта
        /// </summary>
        /// <param name="id">Id акта</param>
        /// <param name="docId">Id скан.документа</param>
        /// <returns></returns>
        [HttpDelete("{id:int}/docs/{docId}")]
        [Authorize(Roles.ADMIN)]
        public async Task<ApiResponse> DeleteActDoc(int id, int docId)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await actSvc.DeleteActDoc(id, docId);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }

            return apiResponse;
        }

        /// <summary>
        /// Подписание акта
        /// </summary>
        /// <param name="id">Id акта</param>
        /// <param name="extension">Расширение файла</param>
        /// <param name="data">Подписанный документ</param>
        /// <returns></returns>
        [HttpPut("{id:int}/sign")]
        //[Consumes("application/octet-stream")]  для приема именно в byte[], а не в base64
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Act), nameof(id))]
        public async Task<ApiResponse> SignActById(int id, [FromQuery][Required] string extension, [FromBody] byte[] data)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await actSvc.SignActId(id, extension, data);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Убрать подписание акта
        /// </summary>
        [HttpPut("{id:int}/unsign")]
        [Authorize(Roles.ADMIN)]
        public async Task<ApiResponse> UnsignActById(int id)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await actSvc.UnsignActId(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Создать электронный счет-фактуру
        /// </summary>
        /// <param name="id">Id акта</param>
        /// <returns></returns>
        [HttpPut("{id:int}/create-esf")]
        [Authorize(Roles.ADMIN)]
        public async Task<ApiResponse<EsfUploadResponse>> CreateEsf(int id)
        {
            var apiResponse = new ApiResponse<EsfUploadResponse>();
            try
            {
                var act = await actSvc.GetActById(id);
                apiResponse = await actSvc.CreateEsf(act);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }
    }
}
