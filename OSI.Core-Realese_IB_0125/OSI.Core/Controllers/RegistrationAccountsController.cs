using Microsoft.AspNetCore.Mvc;
using OSI.Core.Auth;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using OSI.Core.Services;
using System.Threading.Tasks;
using System;
using OSI.Core.Models;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Счета в заявке
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationAccountsController : ControllerBase
    {
        private readonly IRegistrationAccountSvc registrationAccountSvc;

        public RegistrationAccountsController(IRegistrationAccountSvc registrationAccountSvc)
        {
            this.registrationAccountSvc = registrationAccountSvc;
        }

        /// <summary>
        /// Получить данные по счету
        /// </summary>
        /// <param name="id">Id счета</param>
        /// <response code="200">RegistrationAccount</response>
        [HttpGet("{id:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(RegistrationAccount), nameof(id))]
        public async Task<ApiResponse<RegistrationAccount>> GetRegistrationAccountById(int id)
        {
            var apiResponse = new ApiResponse<RegistrationAccount>();
            try
            {
                RegistrationAccount model = await registrationAccountSvc.GetRegistrationAccountById(id);
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
        /// <response code="200">RegistrationAccount</response>
        [HttpPost]
        [Authorize]
        [UserHasAccessFilter(typeof(Registration), nameof(request), nameof(RegistrationAccountRequest.RegistrationId))]
        public async Task<ApiResponse<RegistrationAccount>> AddRegistrationAccount(RegistrationAccountRequest request)
        {
            var apiResponse = new ApiResponse<RegistrationAccount>();
            try
            {
                RegistrationAccount model = await registrationAccountSvc.AddOrUpdateRegistrationAccount(0, request);
                apiResponse.Result = model;
            }
            catch (Exception ex)
            {
                apiResponse.FromError(registrationAccountSvc.GetExceptionMessage(ex));
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
        [Authorize]
        [UserHasAccessFilter(typeof(Registration), nameof(request), nameof(RegistrationAccountRequest.RegistrationId))]
        [UserHasAccessFilter(typeof(RegistrationAccount), nameof(id))]
        public async Task<ApiResponse> UpdateRegistrationAccount(int id, RegistrationAccountRequest request)
        {
            var apiResponse = new ApiResponse();
            try
            {
                _ = await registrationAccountSvc.AddOrUpdateRegistrationAccount(id, request);
            }
            catch (Exception ex)
            {
                apiResponse.FromError(registrationAccountSvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Удаление счета
        /// </summary>
        /// <param name="id">Id счета</param>
        [HttpDelete("{id:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(RegistrationAccount), nameof(id))]
        public async Task<ApiResponse> DeleteRegistrationAccount(int id)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await registrationAccountSvc.DeleteRegistrationAccount(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }
    }
}
