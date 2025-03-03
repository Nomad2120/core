using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Responses;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Авторизация
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthSvc authService;

        public AuthController(IAuthSvc authService)
        {
            this.authService = authService;
        }

        /// <summary>
        /// Авторизация пользователя
        /// </summary>
        /// <param name="username">Имя пользователя (для председателей это номер телефона)</param>
        /// <param name="password">Постоянный пароль</param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<ApiResponse<AuthorizeResponse>> Authorize(string username, string password)
        {
            var apiResponse = new ApiResponse<AuthorizeResponse>();
            try
            {
                var authorizeResponse = await authService.Authorize(username, password);
                apiResponse.Result = authorizeResponse;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Обновление токена пользователя
        /// </summary>
        /// <returns></returns>
        [HttpPost("refresh")]
        [Authorize(Roles.All)]
        public ApiResponse<string> Refresh()
        {
            var apiResponse = new ApiResponse<string>();
            try
            {
                apiResponse.Result = authService.GenerateJwtToken(User.Claims);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Проверить контактные данные
        /// </summary>
        /// <param name="phone">Телефон в виде 7ххххххххх</param>
        /// <returns></returns>
        [HttpGet("check-contact/{phone}")]
        [AllowAnonymous]
        public async Task<ApiResponse<UserContactResponse>> CheckContact([RegularExpression(@"^7\d{9}$", ErrorMessage = "Укажите номер телефона в виде 7ххххххххх")] string phone)
        {
            var apiResponse = new ApiResponse<UserContactResponse>();
            try
            {
                apiResponse.Result = await authService.GetUserContact(phone);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Генерация 6-тизначного кода (ОТП) и отправка в телеграм/sms по данному номеру
        /// </summary>
        /// <param name="phone">Телефон в виде 7ххххххххх</param>
        /// <returns></returns>
        [HttpGet("generate-otp/{phone}")]
        [AllowAnonymous]
        public async Task<ApiResponse> GenerateOtp([RegularExpression(@"^7\d{9}$", ErrorMessage = "Укажите номер телефона в виде 7ххххххххх")] string phone)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await authService.GenerateOTP(phone);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Проверка 6-тизначного кода (ОТП)
        /// </summary>
        /// <param name="phone">Телефон в виде 7ххххххххх</param>
        /// <param name="otp">Код</param>
        /// <response code="200">Id пользователя</response>
        [HttpGet("verify-otp/{phone}/{otp}")]
        [AllowAnonymous]
        public async Task<ApiResponse<AuthorizeResponse>> VerifyOtp(
            [RegularExpression(@"^7\d{9}$", ErrorMessage = "Укажите номер телефона в виде 7ххххххххх")] string phone, 
            [RegularExpression(@"^\d{6}$", ErrorMessage = "Укажите 6-тизначный проверочный код")] string otp)
        {
            var apiResponse = new ApiResponse<AuthorizeResponse>();
            try
            {
                var authorizeResponse = await authService.VerifyOTP(phone, otp);
                apiResponse.Result = authorizeResponse;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Удалить контакт из бота
        /// </summary>
        /// <param name="phone">Телефон в виде 7ххххххххх</param>
        /// <returns></returns>
        [HttpGet("clear-contact/{phone}")]
        [Authorize(Roles.ADMIN)]
        public async Task<ApiResponse> ClearContact([RegularExpression(@"^7\d{9}$", ErrorMessage = "Укажите номер телефона в виде 7ххххххххх")] string phone)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await authService.ClearUserContact(phone);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }
    }
}
