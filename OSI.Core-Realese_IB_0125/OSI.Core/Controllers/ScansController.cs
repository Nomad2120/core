using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Сканированные документы
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ScansController : ControllerBase
    {
        private readonly IScanSvc scanSvc;

        public ScansController(IScanSvc scanSvc)
        {
            this.scanSvc = scanSvc;
        }


        /// <summary>
        /// Получение данных сканированного документа в виде base64-строки
        /// </summary>
        /// <param name="id">Id сканированного документа</param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(Scan), nameof(id))]
        public async Task<ApiResponse<string>> GetScanByteDataById(int id)
        {
            var apiResponse = new ApiResponse<string>();
            try
            {
                apiResponse.Result = await scanSvc.GetScanByteDataById(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        ///// <summary>
        ///// Получение списка всех сканов
        ///// </summary>
        ///// <returns></returns>
        //[HttpGet]
        //public async Task<IEnumerable<Scan>> GetScans()
        //{
        //    return await _scanService.GetModels();
        //}

        ///// <summary>
        ///// Добавление скана
        ///// </summary>
        ///// <param name="model"></param>
        ///// <returns></returns>
        //[HttpPost]
        //public async Task<IActionResult> Post(Scan model)
        //{
        //    try
        //    {
        //        await _scanService.AddOrUpdateModel(model);
        //        return Ok(ModelState);
        //    }
        //    catch (Exception ex)
        //    {
        //        ModelState.AddModelError("", model.GetExceptionMessage(ex));
        //    }

        //    return BadRequest(ModelState);
        //}
    }
}
