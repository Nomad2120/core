using ESoft.CommonLibrary;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OSI.Core.Helpers;
using OSI.Core.Auth;
using OSI.Core.Models.Db;
using OSI.Core.Models.Responses;
using OSI.Core.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IAuthSvc
    {
        string GenerateJwtToken(IEnumerable<Claim> claims);
        Task<AuthorizeResponse> Authorize(string username, string password);
        Task<UserContactResponse> GetUserContact(string phone);
        Task Login(string username, string password);
        Task Logout();
        ClaimsPrincipal ValidateToken(string token, out SecurityToken validatedToken);
        Task GenerateOTP(string phone);
        Task<Models.Responses.AuthorizeResponse> VerifyOTP(string phone, string otp);
        Task ClearUserContact(string phone);
    }

    public class AuthSvc : IAuthSvc
    {
        private readonly TokenServerAuthenticationStateProvider authenticationStateProvider;
        private readonly IConfiguration configuration;
        private readonly ITelegramBotSvc telegramBotService;
        private readonly ISmsSvc smsSvc;
        private readonly IOTPSvc otpService;
        private readonly IServiceProvider serviceProvider;

        public AuthSvc(IServiceProvider serviceProvider, IConfiguration configuration, AuthenticationStateProvider authenticationStateProvider, ITelegramBotSvc telegramBotService, ISmsSvc smsSvc, IOTPSvc otpService)
        {
            this.serviceProvider = serviceProvider;
            if (!(authenticationStateProvider is TokenServerAuthenticationStateProvider))
                throw new ArgumentException($"Не зарегистрирован TokenServerAuthenticationStateProvider", nameof(authenticationStateProvider));
            this.authenticationStateProvider = authenticationStateProvider as TokenServerAuthenticationStateProvider;
            this.configuration = configuration;
            this.telegramBotService = telegramBotService;
            this.smsSvc = smsSvc;
            this.otpService = otpService;
        }

        private async Task<IEnumerable<Claim>> GetClaimsByUser(User user)
        {
            using var db = OSIBillingDbContext.DbContext;
            List<Claim> claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.UserData, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Code),
                new Claim(ClaimTypes.Name, user.FIO)
            };
            // перезапрос ролей, т.к. при верификации ОТП добавляется юзер и роли к нему
            var userRoles = await (from ur in db.UserRoles
                                   join r in db.Roles on ur.RoleId equals r.Id
                                   where ur.UserId == user.Id
                                   select r).ToListAsync();
            foreach (Role role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Code));
            }
            return claims;
        }

        public async Task<AuthorizeResponse> Authorize(string username, string password)
        {
            var response = new AuthorizeResponse
            {
                Token = "",
                UserId = 0
            };
            using var db = OSIBillingDbContext.DbContext;

            if (string.IsNullOrEmpty(password))
                throw new Exception("Не указан пароль");

            string encryptPassword = HashHelper.GetHash<SHA256>(password);

            var user = await db
                .Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Code == username && u.Password == encryptPassword);

            if (user != null)
            {
                var claims = await GetClaimsByUser(user);
                response.Token = GenerateJwtToken(claims);
                response.UserId = user.Id;
            }
            else throw new Exception("Неверный логин или пароль");

            return response;
        }

        public ClaimsPrincipal ValidateToken(string token, out SecurityToken validatedToken)
        {
            validatedToken = null;
            try
            {
                if (!string.IsNullOrEmpty(token))
                {
                    var tokenValidationParameters = serviceProvider.GetRequiredService<IOptionsSnapshot<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters.Clone();
                    var principal = new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out validatedToken);
                    return principal;
                }
            }
            catch { }
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        public string GenerateJwtToken(IEnumerable<Claim> claims)
        {
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Token:Secret"]));
            SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: configuration["Token:Issuer"],
                audience: configuration["Token:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddSeconds(configuration.GetValue<double>("Token:Expires")),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // для админки
        public async Task Login(string username, string password)
        {
            var authorizeResponse = await Authorize(username, password);
            await authenticationStateProvider.SetToken(authorizeResponse.Token);
        }

        public async Task Logout()
        {
            await authenticationStateProvider.SetToken(null);
        }

        public async Task<UserContactResponse> GetUserContact(string phone)
        {
            var response = new UserContactResponse
            {
                IsContacted = false,
                IsRegistered = false,
                HasPassword = false,
                BotUrl = configuration.GetValue<string>("Bot:Url"),
                ChatId = 0
            };
            using var db = OSIBillingDbContext.DbContext;
            var chat = await db.TelegramChats.FirstOrDefaultAsync(t => t.Phone == phone);
            if (chat != null)
            {
                response.IsContacted = true;
                response.BotUrl = "";
                response.ChatId = chat.ChatId;
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Code == phone);
            if (user != null)
            {
                response.IsRegistered = true;
                if (!string.IsNullOrEmpty(user.Password))
                    response.HasPassword = true;
            }

            return response;
        }

        public async Task GenerateOTP(string phone)
        {
            var userContact = await GetUserContact(phone);
            if (userContact.IsContacted)
            {
                await telegramBotService.GenerateAndSendOtp(userContact.ChatId, phone);
            }
            else
            {
                await smsSvc.GenerateAndSendOtp(phone);
            }
        }

        public async Task<AuthorizeResponse> VerifyOTP(string phone, string otp)
        {
            var response = new AuthorizeResponse
            {
                Token = "",
                UserId = 0
            };

            //var userContact = await GetUserContact(phone);
            //if (!userContact.IsContacted) throw new Exception("Данный номер не зарегистрирован в телеграм-боте");

            // не забыть убрать
            if (otp == "123123" || otpService.VerifyOTP(phone, otp))
            {
                using var db = OSIBillingDbContext.DbContext;

                var user = await db.Users
                    //.Include(u => u.UserRoles)
                    //.ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Code == phone);

                if (user == null)
                {
                    user = new User
                    {
                        Code = phone,
                        Phone = phone,
                        FIO = "",
                        Password = ""
                    };
                    db.Users.Add(user);
                    await db.SaveChangesAsync();

                    var role = await db.Roles.FirstOrDefaultAsync(r => r.Code == "ABONENT");
                    if (role != null)
                    {
                        db.UserRoles.Add(new UserRole
                        {
                            RoleId = role.Id,
                            UserId = user.Id
                        });
                        await db.SaveChangesAsync();
                    }
                    else throw new Exception("Не найдена роль абонента");
                }

                var claims = await GetClaimsByUser(user);
                response.UserId = user.Id;
                response.Token = GenerateJwtToken(claims);
            }
            else throw new Exception("Код неверен");

            return response;
        }

        /// <summary>
        /// Удаление контакта телеграм
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public async Task ClearUserContact(string phone)
        {
            using var db = OSIBillingDbContext.DbContext;
            var chat = await db.TelegramChats.FirstOrDefaultAsync(t => t.Phone == phone);
            if (chat != null)
            {
                db.TelegramChats.Remove(chat);
                await db.SaveChangesAsync();
            }
            else throw new Exception("Контакт не найден");
        }
    }
}
