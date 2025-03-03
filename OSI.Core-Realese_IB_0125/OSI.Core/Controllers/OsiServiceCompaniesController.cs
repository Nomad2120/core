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
    /// Сервисные компании ОСИ
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OsiServiceCompaniesController : ControllerBase
    {
        private readonly IOsiServiceCompanySvc osiServiceCompanySvc;

        public OsiServiceCompaniesController(IOsiServiceCompanySvc osiServiceCompanySvc)
        {
            this.osiServiceCompanySvc = osiServiceCompanySvc;
        }

        /// <summary>
        /// Получить данные сервисной компании ОСИ
        /// </summary>
        /// <param name="id">Id сервисной компании</param>
        /// <response code="200">Сервисная компания</response>
        [HttpGet("{id:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(OsiServiceCompany), nameof(id))]
        public async Task<ApiResponse<OsiServiceCompany>> GetOsiServiceCompanyById(int id)
        {
            var apiResponse = new ApiResponse<OsiServiceCompany>();
            try
            {
                OsiServiceCompany model = await osiServiceCompanySvc.GetOsiServiceCompanyById(id);
                apiResponse.Result = model;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Добавить сервисную компанию ОСИ
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(request), nameof(OsiServiceCompanyRequest.OsiId))]
        public async Task<ApiResponse<OsiServiceCompany>> AddOsiServiceCompany(OsiServiceCompanyRequest request)
        {
            var apiResponse = new ApiResponse<OsiServiceCompany>();
            try
            {
                OsiServiceCompany model = await osiServiceCompanySvc.AddOrUpdateOsiServiceCompany(0, request);
                apiResponse.Result = model;
            }
            catch (Exception ex)
            {
                apiResponse.FromError(osiServiceCompanySvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Изменить сервисную компанию ОСИ
        /// </summary>
        /// <param name="id">Id сервисной компании</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles.ADMIN | Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(Osi), nameof(request), nameof(OsiServiceCompanyRequest.OsiId))]
        [UserHasAccessFilter(typeof(OsiServiceCompany), nameof(id))]
        public async Task<ApiResponse> UpdateOsiServiceCompany(int id, OsiServiceCompanyRequest request)
        {
            var apiResponse = new ApiResponse();
            try
            {
                _ = await osiServiceCompanySvc.AddOrUpdateOsiServiceCompany(id, request);
            }
            catch (Exception ex)
            {
                apiResponse.FromError(osiServiceCompanySvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Удаление сервисной компании
        /// </summary>
        /// <param name="id">Id сервисной компании</param>
        [HttpDelete("{id:int}")]
        [Authorize(Roles.ADMIN | Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(OsiServiceCompany), nameof(id))]
        public async Task<ApiResponse> DeleteOsiServiceCompany(int id)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await osiServiceCompanySvc.DeleteOsiServiceCompany(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }
    }
}
