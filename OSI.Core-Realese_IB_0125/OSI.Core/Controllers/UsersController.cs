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
using System.Security.Claims;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Пользователи, они же председатели
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserSvc userSvc;
        private readonly IRegistrationSvc registrationSvc;
        private readonly IOsiSvc osiSvc
            ;

        public UsersController(IUserSvc userService, IRegistrationSvc registrationSvc, IOsiSvc osiSvc)
        {
            this.userSvc = userService;
            this.registrationSvc = registrationSvc;
            this.osiSvc = osiSvc;
        }

        /// <summary>
        /// Получить данные пользователя по токену
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), 200)]
        public async Task<IActionResult> GetUserByToken()
        {
            var apiResponse = new ApiResponse<UserResponse>();
            try
            {
                Claim userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData);
                if (userId != null)
                {
                    var userInfo = await userSvc.GetUserInfoById(int.Parse(userId.Value));
                    apiResponse.Result = userInfo;
                }
                else
                {
                    return Unauthorized();
                }
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return Ok(apiResponse);
        }

        /// <summary>
        /// Получить данные пользователя по Id
        /// </summary>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(User), nameof(id))]
        public async Task<ApiResponse<UserResponse>> GetUserById(int id)
        {
            var apiResponse = new ApiResponse<UserResponse>();
            try
            {
                var userInfo = await userSvc.GetUserInfoById(id);
                apiResponse.Result = userInfo;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получить данные пользователя по коду (он же телефон)
        /// </summary>
        /// <returns></returns>
        [HttpGet("{code}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ApiResponse<UserResponse>> GetUserByCode(string code)
        {
            var apiResponse = new ApiResponse<UserResponse>();
            try
            {
                var userInfo = await userSvc.GetUserInfoByCode(code);
                apiResponse.Result = userInfo;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Изменение данных пользователя
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("{id:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(User), nameof(id), Roles = Roles.ADMIN)]
        public async Task<ApiResponse> UpdateUser(int id, UserRequest model)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await userSvc.UpdateUser(id, model);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получить все заявки пользователя
        /// </summary>
        /// <param name="id">Id пользователя</param>
        /// <response code="200">Список Registration</response>
        [HttpGet("{id:int}/registrations")]
        [Authorize]
        [UserHasAccessFilter(typeof(User), nameof(id))]
        public async Task<ApiResponse<IEnumerable<Registration>>> GetRegistrationsByUserId(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<Registration>>();
            try
            {
                var registrations = await registrationSvc.GetRegistrationsByUserId(id);
                apiResponse.Result = registrations;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получить все активные ОСИ пользователя
        /// </summary>
        /// <param name="id">Id пользователя</param>
        /// <response code="200">Список Osi</response>
        [HttpGet("{id:int}/osi")]
        [Authorize]
        [UserHasAccessFilter(typeof(User), nameof(id))]
        public async Task<ApiResponse<IEnumerable<Osi>>> GetActiveOsiByUserId(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<Osi>>();
            try
            {
                var osi = HttpContext.User.IsInRole("ADMIN") ? await osiSvc.GetOsies() : await osiSvc.GetActiveOsiByUserId(id);
                apiResponse.Result = osi;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Изменение пароля пользователя
        /// </summary>
        /// <param name="id">Id пользователя</param>
        /// <param name="userChangePassword"></param>
        /// <returns></returns>
        [HttpPut("{id:int}/change-password")]
        [Authorize]
        [UserHasAccessFilter(typeof(User), nameof(id), Roles = Roles.ADMIN)]
        public async Task<ApiResponse> ChangePassword(int id, ChangePasswordRequest userChangePassword)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await userSvc.ChangePassword(id, userChangePassword);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Изменение пароля пользователя по ОТП
        /// </summary>
        /// <param name="id">Id пользователя</param>
        /// <param name="userChangePassword"></param>
        /// <returns></returns>
        [HttpPut("{id:int}/reset-password")]
        [Authorize]
        [UserHasAccessFilter(typeof(User), nameof(id), Roles = Roles.ADMIN)]
        public async Task<ApiResponse> ResetPassword(int id, ResetPasswordRequest userChangePassword)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await userSvc.ResetPassword(id, userChangePassword);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Установить постоянный пароль для пользователя
        /// </summary>
        /// <param name="id">Id пользователя</param>
        /// <param name="password">Пароль</param>
        /// <returns></returns>
        [HttpPut("{id:int}/set-password")]
        [Authorize]
        [UserHasAccessFilter(typeof(User), nameof(id), Roles = Roles.ADMIN)]
        public async Task<ApiResponse> SetPermanentPassword(int id, [FromBody] string password)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await userSvc.SetPermanentPassword(id, password);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Удаление всей информации по пользователю: пользователь, контакты, заявки, ОСИ, услуги, абоненты, планы и пр.
        /// </summary>
        /// <param name="id">Id пользователя</param>
        /// <param name="password">Пароль "удалить" на англ. раскладке</param>
        /// <returns></returns>
        [HttpPost("{id:int}/clear-all-information")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ApiResponse> ClearAllInformation(int id, [FromQuery][Required] string password)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await userSvc.ClearAllAboutUser(id, password);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получение привязанных помещений пользователя по его номеру телефона
        /// </summary>
        /// <param name="id">Id пользователя</param>
        /// <returns></returns>
        [HttpGet("{id:int}/affiliations")]
        [Authorize]
        [UserHasAccessFilter(typeof(User), nameof(id))]
        public async Task<ApiResponse<IEnumerable<UserAffiliation>>> GetUserAffiliations(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<UserAffiliation>>();
            try
            {
                var list = await userSvc.GetAffiliationsByUserId(id);
                apiResponse.Result = list;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Список активных председателей
        /// </summary>
        /// <returns></returns>
        [HttpGet("active-chairmans")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ApiResponse<IEnumerable<User>>> GetActiveChairmans()
        {
            var apiResponse = new ApiResponse<IEnumerable<User>>();
            try
            {
                var list = await userSvc.GetActiveChairmans();
                apiResponse.Result = list;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        //[HttpGet]
        //public async Task<IEnumerable<User>> GetUsers()
        //{
        //    return await _userService.GetModels();
        //}

        //[HttpPost]
        //public async Task<IActionResult> Post(User model)
        //{
        //    try
        //    {
        //        await _userService.AddOrUpdateModel(model);
        //        return Ok(ModelState);
        //    }
        //    catch (Exception ex)
        //    {
        //        ModelState.AddModelError("", model.GetExceptionMessage(ex));
        //    }
        //    return BadRequest(ModelState);
        //}

        //[HttpPost]
        //public async Task<IActionResult> Post(User model)
        //{
        //    var validation = _userService.IsModelValid(model);
        //    if (validation.Code == -1)
        //    {
        //        foreach (var error in validation.Result)
        //        {
        //            foreach (var msg in error.Value)
        //            {
        //                ModelState.AddModelError(error.Key, msg);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        await _userService.AddOrUpdateModel(model);
        //        return Ok(ModelState);
        //    }

        //    return BadRequest(ModelState);
        //}
    }
}
