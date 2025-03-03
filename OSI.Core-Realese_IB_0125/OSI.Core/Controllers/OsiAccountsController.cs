using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Счета ОСИ
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OsiAccountsController : ControllerBase
    {
        private readonly IOsiAccountSvc osiAccountSvc;

        public OsiAccountsController(IOsiAccountSvc osiAccountSvc)
        {
            this.osiAccountSvc = osiAccountSvc;
        }

        /// <summary>
        /// Получить данные по счету
        /// </summary>
        /// <param name="id">Id счета</param>
        /// <response code="200">OsiAccount</response>
        [HttpGet("{id:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(OsiAccount), nameof(id))]
        public async Task<ApiResponse<OsiAccount>> GetOsiAccountById(int id)
        {
            var apiResponse = new ApiResponse<OsiAccount>();
            try
            {
                OsiAccount model = await osiAccountSvc.GetOsiAccountById(id);
                apiResponse.Result = model;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Добавить счет
        /// </summary>
        /// <param name="request"></param>        
        /// <response code="200">OsiAccount</response>
        [HttpPost]
        [Authorize(Roles.Support)]
        [UserHasAccessFilter(typeof(Osi), nameof(request), nameof(OsiAccountRequest.OsiId))]
        public async Task<ApiResponse<OsiAccount>> AddOsiAccount(OsiAccountRequest request)
        {
            var apiResponse = new ApiResponse<OsiAccount>();
            try
            {
                OsiAccount model = await osiAccountSvc.AddOrUpdateOsiAccount(0, request);
                apiResponse.Result = model;
            }
            catch (Exception ex)
            {
                apiResponse.FromError(osiAccountSvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Изменить счет
        /// </summary>
        /// <param name="id">Id счета</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles.Support)]
        [UserHasAccessFilter(typeof(Osi), nameof(request), nameof(OsiAccountRequest.OsiId))]
        [UserHasAccessFilter(typeof(OsiAccount), nameof(id))]
        public async Task<ApiResponse> UpdateOsiAccount(int id, OsiAccountRequest request)
        {
            var apiResponse = new ApiResponse();
            try
            {
                _ = await osiAccountSvc.AddOrUpdateOsiAccount(id, request);
            }
            catch (Exception ex)
            {
                apiResponse.FromError(osiAccountSvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Удаление счета
        /// </summary>
        /// <param name="id">Id счета</param>
        [HttpDelete("{id:int}")]
        [Authorize(Roles.Support)]
        [UserHasAccessFilter(typeof(OsiAccount), nameof(id))]
        public async Task<ApiResponse> DeleteOsiAccount(int id)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await osiAccountSvc.DeleteOsiAccount(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }
    }
}
