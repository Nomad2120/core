using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OSI.Core.Models.Db;
using ESoft.CommonLibrary;
using System.Security.Cryptography;
using OSI.Core.Models.Responses;
using OSI.Core.Models.Requests;

namespace OSI.Core.Services
{
    public interface IUserSvc
    {
        Task<User> GetUserById(int id);

        Task<User> GetUserByCode(string code);

        Task<UserResponse> GetUserInfoById(int id);

        Task<UserResponse> GetUserInfoByCode(string code);

        Task<IEnumerable<User>> GetUsers();
        Task<IEnumerable<User>> GetUsersByRole(string roleCode);
        Task<IEnumerable<User>> GetActiveChairmans();

        Task AddOrUpdateModel(User model);

        Task ChangePassword(int id, ChangePasswordRequest userChangePassword);

        Task ResetPassword(int id, ResetPasswordRequest userChangePassword);

        Task UpdateUser(int id, UserRequest model);

        Task SetPermanentPassword(int id, string notEncryptedPassword);

        Task ClearAllAboutUser(int userId, string password);

        Task<IEnumerable<UserAffiliation>> GetAffiliationsByUserId(int id);
    }

    public class UserSvc : IUserSvc
    {
        private readonly IOTPSvc otpService;

        public UserSvc(IOTPSvc otpService)
        {
            this.otpService = otpService;
        }

        public async Task<IEnumerable<User>> GetUsers()
        {
            using var db = OSIBillingDbContext.DbContext;
            return await db.Users.ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersByRole(string roleCode)
        {
            using var db = OSIBillingDbContext.DbContext;
            return await db.Users.Where(u => u.UserRoles.Any(ur => ur.Role.Code == roleCode)).ToListAsync();
        }

        public async Task<IEnumerable<User>> GetActiveChairmans()
        {
            using var db = OSIBillingDbContext.DbContext;
            return await db.Users
                .Where(u => u.UserRoles.Any(ur => ur.Role.Code == "CHAIRMAN"))
                .Where(u => u.OsiUsers.Any(ou => ou.Osi.IsLaunched && ou.Osi.Address.StartsWith("г. Актобе")))
                .ToListAsync();
        }

        public async Task<User> GetUserById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            //var user = await db
            //                .Users
            //                .Include(u => u.UserRoles)
            //                .ThenInclude(ur => ur.Role)
            //                .FirstOrDefaultAsync(u => u.Id == id);
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                throw new Exception("Пользователь не найден");
            return user;
        }

        public async Task<User> GetUserByCode(string code)
        {
            using var db = OSIBillingDbContext.DbContext;
            var user = await db.Users.FirstOrDefaultAsync(m => m.Code == code.ToUpper());
            if (user == null)
                throw new Exception("Пользователь не найден");
            return user;
        }

        public async Task ChangePassword(int id, ChangePasswordRequest userChangePassword)
        {
            using var db = OSIBillingDbContext.DbContext;
            User user = await GetUserById(id);
            string encryptOldPassword = HashHelper.GetHash<SHA256>(userChangePassword.OldPassword);
            if (user.Password != encryptOldPassword)
                throw new Exception("Старый пароль не верен");

            if (userChangePassword.ConfirmPassword != userChangePassword.NewPassword)
                throw new Exception("Пароли не совпадают");

            if (userChangePassword.OldPassword == userChangePassword.NewPassword)
                throw new Exception("Новый пароль не изменился");

            string encryptNewPassword = HashHelper.GetHash<SHA256>(userChangePassword.NewPassword);

            user.Password = encryptNewPassword;
            db.Users.Update(user);
            await db.SaveChangesAsync();
        }

        public async Task ResetPassword(int id, ResetPasswordRequest resetPassword)
        {
            using var db = OSIBillingDbContext.DbContext;
            User user = await GetUserById(id);

            // не забыть убрать
            if (resetPassword.Otp != "123123" && !otpService.VerifyOTP(user.Phone, resetPassword.Otp))
                throw new Exception("Код неверен");

            if (resetPassword.ConfirmPassword != resetPassword.NewPassword)
                throw new Exception("Пароли не совпадают");

            string encryptNewPassword = HashHelper.GetHash<SHA256>(resetPassword.NewPassword);

            user.Password = encryptNewPassword;
            db.Users.Update(user);
            await db.SaveChangesAsync();
        }

        public async Task AddOrUpdateModel(User model)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (model.Id == default)
            {
                db.Users.Add(model);
            }
            else
            {
                db.Users.Update(model);
            }
            await db.SaveChangesAsync();
        }

        public async Task<UserResponse> GetUserInfoByCode(string code)
        {
            var user = await GetUserByCode(code);
            return await GetUserInfo(user);
        }

        public async Task<UserResponse> GetUserInfoById(int id)
        {
            var user = await GetUserById(id);
            return await GetUserInfo(user);
        }

        public async Task<UserResponse> GetUserInfo(User user)
        {
            using var db = OSIBillingDbContext.DbContext;
            var response = new UserResponse
            {
                Id = user.Id,
                Code = user.Code,
                Email = user.Email,
                FIO = user.FIO,
                IIN = user.IIN,
                Phone = user.Phone
            };

            response.Roles = await db.UserRoles.Where(ur => ur.UserId == user.Id).Select(ur => new RoleResponse
            {
                NameRu = ur.Role.Name,
                NameKz = ur.Role.NameKz,
                Role = ur.Role.Code
            }).ToListAsync();

            return response;
        }

        public async Task UpdateUser(int id, UserRequest model)
        {
            using var db = OSIBillingDbContext.DbContext;
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                throw new Exception("Пользователь не найден");

            user.IIN = model.IIN;
            user.FIO = model.FIO;
            user.Email = model.Email;

            await AddOrUpdateModel(user);
        }

        public async Task SetPermanentPassword(int id, string notEncryptedPassword)
        {
            var user = await GetUserById(id);
            using var db = OSIBillingDbContext.DbContext;
            if (string.IsNullOrEmpty(user.Password))
            {
                user.Password = HashHelper.GetHash<SHA256>(notEncryptedPassword);
                db.Users.Update(user);
                await db.SaveChangesAsync();
            }
            else
                throw new Exception("У данного пользователя уже установлен постоянный пароль");
        }

        public async Task ClearAllAboutUser(int userId, string password)
        {
            if (password != "elfkbnm")
                throw new Exception("Пароль неверный");

            using var db = OSIBillingDbContext.DbContext;
            var user = await GetUserById(userId);

            var registrations = await db.Registrations.Where(r => r.UserId == user.Id).ToListAsync();
            foreach (Registration registration in registrations)
            {
                var registrationDocs = await db.RegistrationDocs.Where(d => d.RegistrationId == registration.Id).ToListAsync();
                db.RegistrationDocs.RemoveRange(registrationDocs);

                var registrationAccounts = await db.RegistrationAccounts.Where(d => d.RegistrationId == registration.Id).ToListAsync();
                db.RegistrationAccounts.RemoveRange(registrationAccounts);

                var osies = await db.Osies.Where(d => d.RegistrationId == registration.Id).ToListAsync();
                foreach (Osi osi in osies)
                {
                    var osiDocs = await db.OsiDocs.Where(d => d.OsiId == osi.Id).ToListAsync();
                    foreach (OsiDoc doc in osiDocs)
                    {
                        Scan scan = await db.Scans.FirstOrDefaultAsync(s => s.Id == doc.ScanId);
                        if (scan != null)
                            db.Scans.Remove(scan);
                        db.OsiDocs.Remove(doc);
                    }

                    var osiUsers = await db.OsiUsers.Where(d => d.OsiId == osi.Id).ToListAsync();
                    db.OsiUsers.RemoveRange(osiUsers);

                    var osiAccounts = await db.OsiAccounts.Where(d => d.OsiId == osi.Id).ToListAsync();
                    db.OsiAccounts.RemoveRange(osiAccounts);

                    var osiServiceCompanies = await db.OsiServiceCompanies.Where(d => d.OsiId == osi.Id).ToListAsync();
                    db.OsiServiceCompanies.RemoveRange(osiServiceCompanies);

                    var serviceGroupSaldo = await db.ServiceGroupSaldos.Where(d => d.OsiId == osi.Id).ToListAsync();
                    db.ServiceGroupSaldos.RemoveRange(serviceGroupSaldo);

                    var planAccurals = await db.PlanAccurals.Where(d => d.OsiId == osi.Id).ToListAsync();
                    db.PlanAccurals.RemoveRange(planAccurals);

                    var osiServices = await db.OsiServices.Where(d => d.OsiId == osi.Id).ToListAsync();
                    db.OsiServices.RemoveRange(osiServices);

                    var abonents = await db.Abonents.Where(d => d.OsiId == osi.Id).ToListAsync();
                    foreach (Abonent abonent in abonents)
                    {
                        var abonentHistories = await db.AbonentHistories.Where(d => d.AbonentId == abonent.Id).ToListAsync();
                        db.AbonentHistories.RemoveRange(abonentHistories);
                        db.Abonents.Remove(abonent);
                    }

                    var transactions = await db.Transactions.Where(d => d.OsiId == osi.Id).ToListAsync();
                    db.Transactions.RemoveRange(transactions);

                    var accountReports = await db.AccountReports.Where(d => d.OsiId == osi.Id).ToListAsync();
                    foreach (var accountReport in accountReports)
                    {
                        var accountReportListRelations = await db.AccountReportListRelations.Where(d => d.ReportId == accountReport.Id).ToListAsync();
                        db.AccountReportListRelations.RemoveRange(accountReportListRelations);
                        foreach (var listRelation in accountReportListRelations)
                        {
                            var list = await db.AccountReportLists.FirstOrDefaultAsync(d => d.Id == listRelation.ListId);
                            // тут надо смотреть чтобы этот list не использовался в других relations
                            var otherReportRelation = await db.AccountReportListRelations.FirstOrDefaultAsync(d => d.ListId == list.Id && d.ReportId != accountReport.Id);
                            if (otherReportRelation == null)
                            {
                                // чистим items и details
                                var items = await db.AccountReportListItems.Where(d => d.ListId == list.Id).ToListAsync();
                                var details = await db.AccountReportListItemDetails.Where(d => d.Item.ListId == list.Id).ToListAsync();
                                db.AccountReportListItemDetails.RemoveRange(details);
                                db.AccountReportListItems.RemoveRange(items);
                                db.AccountReportLists.Remove(list);  
                            }
                            db.AccountReportListRelations.Remove(listRelation);
                        }
                        var accountReportDocs = await db.AccountReportDocs.Where(d => d.AccountReportId == accountReport.Id).ToListAsync();
                        db.AccountReportDocs.RemoveRange(accountReportDocs);
                    }
                    db.AccountReports.RemoveRange(accountReports);
                    db.Osies.Remove(osi);
                }
                db.Registrations.Remove(registration);
            }

            var userRoles = await db.UserRoles.Where(ur => ur.UserId == user.Id).ToListAsync();
            db.UserRoles.RemoveRange(userRoles);

            var telegramChat = await db.TelegramChats.FirstOrDefaultAsync(t => t.Phone == user.Phone);
            db.TelegramChats.Remove(telegramChat);

            db.Users.Remove(user);

            await db.SaveChangesAsync();
        }

        public async Task<IEnumerable<UserAffiliation>> GetAffiliationsByUserId(int id)
        {
            User user = await GetUserById(id);
            using var db = OSIBillingDbContext.DbContext;
            var abonents = await db.Abonents.Include(a => a.Osi).Where(a => a.Phone == user.Phone).ToListAsync();
            var osies = abonents.GroupBy(a => a.OsiId);

            List<UserAffiliation> affiliations = new List<UserAffiliation>();

            foreach (var osi in osies)
            {
                UserAffiliation affiliation = new()
                {
                    Abonents = new List<UserAffiliationAbonent>()
                };
                foreach (Abonent a in osi)
                {
                    affiliation.OsiId = a.Osi.Id;
                    affiliation.OsiName = a.Osi.Name;
                    affiliation.Address = a.Osi.Address;
                    affiliation.Abonents.Add(new UserAffiliationAbonent
                    {
                        AbonentId = a.Id,
                        AbonentName = a.Name,
                        Flat = a.Flat
                    });
                }
                affiliations.Add(affiliation);
            }

            return affiliations;
        }
    }
}
