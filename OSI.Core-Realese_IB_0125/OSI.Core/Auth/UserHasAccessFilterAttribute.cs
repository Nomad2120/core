using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using OSI.Core.Extensions;
using OSI.Core.Helpers;
using OSI.Core.Models.Db;
using OSI.Core.Pages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace OSI.Core.Auth
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used in release environment")]
    public class UserHasAccessFilterAttribute : ActionFilterAttribute
    {
        private static readonly Dictionary<string, Func<ClaimsPrincipal, int, IEnumerable<object>, Task<bool>>> checkAccess = new();

        private static async Task<bool> CheckAbonentAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int abonentId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .Abonents
                    .Where(a => a.Id == abonentId)
                    .Join(db.Osies, a => a.OsiId, o => o.Id, (a, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .Abonents
                    .Where(a => a.Id == abonentId)
                    .Join(db.Users, a => a.Phone, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckAbonentFlatAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int osiId = (int)values.ElementAt(0);
            string flat = (string)values.ElementAt(1);
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .Abonents
                    .Where(a => a.OsiId == osiId && a.Flat == flat)
                    .Join(db.Osies, a => a.OsiId, o => o.Id, (a, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .Abonents
                    .Where(a => a.OsiId == osiId && a.Flat == flat)
                    .Join(db.Users, a => a.Phone, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckAbonentNumAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            string abonentNum = (string)values.First();
            int abonentId = int.TryParse(abonentNum, out var id) ? id : 0;
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .Abonents
                    .Where(a => a.Id == abonentId || a.ErcAccount == abonentNum)
                    .Join(db.Osies, a => a.OsiId, o => o.Id, (a, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .Abonents
                    .Where(a => a.Id == abonentId || a.ErcAccount == abonentNum)
                    .Join(db.Users, a => a.Phone, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckAccountReportAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int accountReportId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .AccountReports
                    .Where(r => r.Id == accountReportId)
                    .Join(db.Osies, r => r.OsiId, o => o.Id, (r, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .AccountReports
                    .Where(r => r.Id == accountReportId && r.State == Models.Enums.AccountReportStateCodes.PUBLISHED)
                    .Join(db.Osies, r => r.OsiId, o => o.Id, (r, o) => o.Id)
                    .Join(db.Abonents, o => o, a => a.OsiId, (o, a) => a.Phone)
                    .Join(db.Users, a => a, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckAccountReportListAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int accountReportListId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .AccountReportLists
                    .Where(l => l.Id == accountReportListId)
                    .Join(db.AccountReportListRelations, l => l.Id, lr => lr.ListId, (l, lr) => lr.ReportId)
                    .Join(db.AccountReports, lr => lr, r => r.Id, (l, r) => r.OsiId)
                    .Join(db.Osies, r => r, o => o.Id, (r, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .AccountReportLists
                    .Where(l => l.Id == accountReportListId)
                    .Join(db.AccountReportListRelations, l => l.Id, lr => lr.ListId, (l, lr) => lr.ReportId)
                    .Join(db.AccountReports,
                        lr => new { Id = lr, State = Models.Enums.AccountReportStateCodes.PUBLISHED },
                        r => new { r.Id, r.State },
                        (lr, r) => r.OsiId)
                    .Join(db.Osies, r => r, o => o.Id, (r, o) => o.Id)
                    .Join(db.Abonents, o => o, a => a.OsiId, (o, a) => a.Phone)
                    .Join(db.Users, a => a, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckAccountReportListItemAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int accountReportListItemId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .AccountReportListItems
                    .Where(i => i.Id == accountReportListItemId)
                    .Join(db.AccountReportLists, i => i.ListId, l => l.Id, (i, l) => l.Id)
                    .Join(db.AccountReportListRelations, l => l, lr => lr.ListId, (l, lr) => lr.ReportId)
                    .Join(db.AccountReports, lr => lr, r => r.Id, (lr, r) => r.OsiId)
                    .Join(db.Osies, r => r, o => o.Id, (r, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .AccountReportListItems
                    .Where(i => i.Id == accountReportListItemId)
                    .Join(db.AccountReportLists, i => i.ListId, l => l.Id, (i, l) => l.Id)
                    .Join(db.AccountReportListRelations, l => l, lr => lr.ListId, (l, lr) => lr.ReportId)
                    .Join(db.AccountReports,
                        lr => new { Id = lr, State = Models.Enums.AccountReportStateCodes.PUBLISHED },
                        r => new { r.Id, r.State },
                        (lr, r) => r.OsiId)
                    .Join(db.Osies, r => r, o => o.Id, (r, o) => o.Id)
                    .Join(db.Abonents, o => o, a => a.OsiId, (o, a) => a.Phone)
                    .Join(db.Users, a => a, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckAccountReportListItemDetailAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int accountReportListItemDetailId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .AccountReportListItemDetails
                    .Where(d => d.Id == accountReportListItemDetailId)
                    .Join(db.AccountReportListItems, d => d.ItemId, i => i.Id, (d, i) => i.ListId)
                    .Join(db.AccountReportLists, i => i, l => l.Id, (i, l) => l.Id)
                    .Join(db.AccountReportListRelations, l => l, lr => lr.ListId, (l, lr) => lr.ReportId)
                    .Join(db.AccountReports, lr => lr, r => r.Id, (lr, r) => r.OsiId)
                    .Join(db.Osies, r => r, o => o.Id, (r, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .AccountReportListItemDetails
                    .Where(d => d.Id == accountReportListItemDetailId)
                    .Join(db.AccountReportListItems, d => d.ItemId, i => i.Id, (d, i) => i.ListId)
                    .Join(db.AccountReportLists, i => i, l => l.Id, (i, l) => l.Id)
                    .Join(db.AccountReportListRelations, l => l, lr => lr.ListId, (l, lr) => lr.ReportId)
                    .Join(db.AccountReports,
                        lr => new { Id = lr, State = Models.Enums.AccountReportStateCodes.PUBLISHED },
                        r => new { r.Id, r.State },
                        (lr, r) => r.OsiId)
                    .Join(db.Osies, r => r, o => o.Id, (r, o) => o.Id)
                    .Join(db.Abonents, o => o, a => a.OsiId, (o, a) => a.Phone)
                    .Join(db.Users, a => a, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckActAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int actId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .Acts
                    .Where(a => a.Id == actId)
                    .Join(db.Osies, a => a.OsiId, o => o.Id, (a, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .Acts
                    .Where(a => a.Id == actId)
                    .Join(db.Osies, a => a.OsiId, o => o.Id, (a, o) => o.Id)
                    .Join(db.Abonents, o => o, a => a.OsiId, (o, a) => a.Phone)
                    .Join(db.Users, a => a, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckOsiAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int osiId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .Osies
                    .Where(o => o.Id == osiId)
                    .Join(db.OsiUsers, o => o.Id, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .Osies
                    .Where(o => o.Id == osiId)
                    .Join(db.Abonents, o => o.Id, a => a.OsiId, (o, a) => a.Phone)
                    .Join(db.Users, a => a, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckOsiAccountAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int osiAccountId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .OsiAccounts
                    .Where(oa => oa.Id == osiAccountId)
                    .Join(db.Osies, oa => oa.OsiId, o => o.Id, (oa, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .OsiAccounts
                    .Where(oa => oa.Id == osiAccountId)
                    .Join(db.Osies, oa => oa.OsiId, o => o.Id, (oa, o) => o.Id)
                    .Join(db.Abonents, o => o, a => a.OsiId, (o, a) => a.Phone)
                    .Join(db.Users, a => a, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckOsiAccountApplicationAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int osiAccountApplicationId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .OsiAccountApplications
                    .Where(oaa => oaa.Id == osiAccountApplicationId)
                    .Join(db.Osies, oaa => oaa.OsiId, o => o.Id, (oaa, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .OsiAccountApplications
                    .Where(oa => oa.Id == osiAccountApplicationId)
                    .Join(db.Osies, oa => oa.OsiId, o => o.Id, (oa, o) => o.Id)
                    .Join(db.Abonents, o => o, a => a.OsiId, (o, a) => a.Phone)
                    .Join(db.Users, a => a, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckOsiAccountApplicationDocAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int osiAccountApplicationDocId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .OsiAccountApplicationDocs
                    .Where(oaad => oaad.Id == osiAccountApplicationDocId)
                    .Join(db.OsiAccountApplications, oaad => oaad.OsiAccountApplicationId, oaa => oaa.Id, (oaad, oaa) => oaa.OsiId)
                    .Join(db.Osies, oaa => oaa, o => o.Id, (oaa, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .OsiAccountApplicationDocs
                    .Where(oaad => oaad.Id == osiAccountApplicationDocId)
                    .Join(db.OsiAccountApplications, oaad => oaad.OsiAccountApplicationId, oaa => oaa.Id, (oaad, oaa) => oaa.OsiId)
                    .Join(db.Osies, oaa => oaa, o => o.Id, (oaa, o) => o.Id)
                    .Join(db.Abonents, o => o, a => a.OsiId, (o, a) => a.Phone)
                    .Join(db.Users, a => a, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckOsiDocAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int osiDocId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .OsiDocs
                    .Where(od => od.Id == osiDocId)
                    .Join(db.Osies, od => od.OsiId, o => o.Id, (od, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .OsiDocs
                    .Where(od => od.Id == osiDocId)
                    .Join(db.Osies, od => od.OsiId, o => o.Id, (od, o) => o.Id)
                    .Join(db.Abonents, o => o, a => a.OsiId, (o, a) => a.Phone)
                    .Join(db.Users, a => a, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckOsiServiceAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int osiServiceId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .OsiServices
                    .Where(os => os.Id == osiServiceId)
                    .Join(db.Osies, os => os.OsiId, o => o.Id, (os, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .OsiServices
                    .Where(os => os.Id == osiServiceId)
                    .Join(db.Osies, os => os.OsiId, o => o.Id, (os, o) => o.Id)
                    .Join(db.Abonents, o => o, a => a.OsiId, (o, a) => a.Phone)
                    .Join(db.Users, a => a, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckOsiServiceCompanyAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int osiServiceCompanyId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .OsiServiceCompanies
                    .Where(osc => osc.Id == osiServiceCompanyId)
                    .Join(db.Osies, osc => osc.OsiId, o => o.Id, (osc, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .OsiServiceCompanies
                    .Where(osc => osc.Id == osiServiceCompanyId)
                    .Join(db.Osies, osc => osc.OsiId, o => o.Id, (osc, o) => o.Id)
                    .Join(db.Abonents, o => o, a => a.OsiId, (o, a) => a.Phone)
                    .Join(db.Users, a => a, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckPlanAccuralAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int planAccuralId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .PlanAccurals
                    .Where(pa => pa.Id == planAccuralId)
                    .Join(db.Osies, pa => pa.OsiId, o => o.Id, (pa, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .PlanAccurals
                    .Where(pa => pa.Id == planAccuralId)
                    .Join(db.Osies, pa => pa.OsiId, o => o.Id, (pa, o) => o.Id)
                    .Join(db.Abonents, o => o, a => a.OsiId, (o, a) => a.Phone)
                    .Join(db.Users, a => a, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckRegistrationAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int registrationId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .Registrations
                    .Where(r => r.Id == registrationId)
                    .Join(db.Osies, r => r.Id, o => o.RegistrationId, (r, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result)
            {
                result = await db
                    .Registrations
                    .AnyAsync(r => r.Id == registrationId && r.UserId == userId);
            }
            return result;
        }

        private static async Task<bool> CheckRegistrationAccountAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int registrationAccountId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .RegistrationAccounts
                    .Where(ra => ra.Id == registrationAccountId)
                    .Join(db.Osies, ra => ra.RegistrationId, o => o.RegistrationId, (r, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result)
            {
                result = await db
                    .RegistrationAccounts
                    .Where(ra => ra.Id == registrationAccountId)
                    .Join(db.Registrations, ra => ra.RegistrationId, r => r.Id, (ra, r) => r.UserId)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckRegistrationDocAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int registrationDocId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .RegistrationDocs
                    .Where(rd => rd.Id == registrationDocId)
                    .Join(db.Osies, rd => rd.RegistrationId, o => o.RegistrationId, (rd, o) => o.Id)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result)
            {
                result = await db
                    .RegistrationDocs
                    .Where(rd => rd.Id == registrationDocId)
                    .Join(db.Registrations, rd => rd.RegistrationId, r => r.Id, (rd, r) => r.UserId)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static async Task<bool> CheckScanAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int scanId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await (db
                    .Users
                    .Join(db.Registrations, u => u.Id, r => r.UserId, (u, r) => new { u, r })
                    .Join(db.RegistrationDocs, x => x.r.Id, rd => rd.RegistrationId, (x, rd) => new { x.u.Id, rd.ScanId }))
                    .Union(db
                    .Users
                    .Join(db.OsiUsers, u => u.Id, ou => ou.UserId, (u, ou) => new { u, ou })
                    .Join(db.OsiDocs, x => x.ou.OsiId, od => od.OsiId, (x, od) => new { x.u.Id, od.ScanId }))
                    .Union(db
                    .Users
                    .Join(db.OsiUsers, u => u.Id, ou => ou.UserId, (u, ou) => new { u, ou })
                    .Join(db.Acts, x => x.ou.OsiId, a => a.OsiId, (x, a) => new { x.u, a })
                    .Join(db.ActDocs, x => x.a.Id, ad => ad.ActId, (x, ad) => new { x.u.Id, ad.ScanId }))
                    .Union(db
                    .Users
                    .Join(db.OsiUsers, u => u.Id, ou => ou.UserId, (u, ou) => new { u, ou })
                    .Join(db.AccountReports, x => x.ou.OsiId, ar => ar.OsiId, (x, ar) => new { x.u, ar })
                    .Join(db.AccountReportDocs, x => x.ar.Id, ard => ard.AccountReportId, (x, ard) => new { x.u.Id, ard.ScanId }))
                    .AnyAsync(x => x.Id == userId && x.ScanId == scanId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await (db
                    .Users
                    .Join(db.Registrations, u => u.Id, r => r.UserId, (u, r) => new { u, r })
                    .Join(db.RegistrationDocs, x => x.r.Id, rd => rd.RegistrationId, (x, rd) => new { x.u.Id, rd.ScanId }))
                    .Union(db
                    .Users
                    .Join(db.Abonents, u => u.Phone, a => a.Phone, (u, a) => new { u, a })
                    .Join(db.OsiDocs, x => x.a.OsiId, od => od.OsiId, (x, od) => new { x.u.Id, od.ScanId }))
                    .Union(db
                    .Users
                    .Join(db.Abonents, u => u.Phone, a => a.Phone, (u, a) => new { u, a })
                    .Join(db.AccountReports, x => x.a.OsiId, ar => ar.OsiId, (x, ar) => new { x.u, ar })
                    .Join(db.AccountReportDocs, x => x.ar.Id, ard => ard.AccountReportId, (x, ard) => new { x.u.Id, ard.ScanId }))
                    .AnyAsync(x => x.Id == userId && x.ScanId == scanId);
            }
            return result;
        }

        private static async Task<bool> CheckServiceGroupSaldoAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int serviceGroupSaldoId = (int)values.First();
            using var db = OSIBillingDbContext.DbContext;
            var result = false;
            if (user.IsInRole("CHAIRMAN"))
            {
                result = await db
                    .ServiceGroupSaldos
                    .Where(sgs => sgs.Id == serviceGroupSaldoId)
                    .Join(db.Abonents, sgs => new { Id = sgs.AbonentId, sgs.OsiId }, a => new { a.Id, a.OsiId }, (sgs, a) => a.OsiId)
                    .Join(db.OsiUsers, o => o, ou => ou.OsiId, (o, ou) => ou.UserId)
                    .AnyAsync(u => u == userId);
            }
            if (!result && user.IsInRole("ABONENT"))
            {
                result = await db
                    .ServiceGroupSaldos
                    .Where(sgs => sgs.Id == serviceGroupSaldoId)
                    .Join(db.Abonents, sgs => new { Id = sgs.AbonentId, sgs.OsiId }, a => new { a.Id, a.OsiId }, (sgs, a) => a.Phone)
                    .Join(db.Users, a => a, u => u.Phone, (a, u) => u.Id)
                    .AnyAsync(u => u == userId);
            }
            return result;
        }

        private static Task<bool> CheckUserAccess(ClaimsPrincipal user, int userId, IEnumerable<object> values)
        {
            int id = (int)values.First();
            return Task.FromResult(userId == id);
        }

        static UserHasAccessFilterAttribute()
        {
#if !DEBUG || ENABLE_AUTH //Выключаем проверки на принадлежность объектов пользователю, если запускаемся локально и не объявлена константа ENABLE_AUTH
            checkAccess[typeof(Abonent).Name] = CheckAbonentAccess;
            checkAccess[typeof(Abonent).Name + "Flat"] = CheckAbonentFlatAccess;
            checkAccess[typeof(Abonent).Name + "Num"] = CheckAbonentNumAccess;
            checkAccess[typeof(AccountReport).Name] = CheckAccountReportAccess;
            checkAccess[typeof(AccountReportList).Name] = CheckAccountReportListAccess;
            checkAccess[typeof(AccountReportListItem).Name] = CheckAccountReportListItemAccess;
            checkAccess[typeof(AccountReportListItemDetail).Name] = CheckAccountReportListItemDetailAccess;
            checkAccess[typeof(Act).Name] = CheckActAccess;
            checkAccess[typeof(Osi).Name] = CheckOsiAccess;
            checkAccess[typeof(OsiAccount).Name] = CheckOsiAccountAccess;
            checkAccess[typeof(OsiAccountApplication).Name] = CheckOsiAccountApplicationAccess;
            checkAccess[typeof(OsiAccountApplicationDoc).Name] = CheckOsiAccountApplicationDocAccess;
            checkAccess[typeof(OsiDoc).Name] = CheckOsiDocAccess;
            checkAccess[typeof(OsiService).Name] = CheckOsiServiceAccess;
            checkAccess[typeof(OsiServiceCompany).Name] = CheckOsiServiceCompanyAccess;
            checkAccess[typeof(PlanAccural).Name] = CheckPlanAccuralAccess;
            checkAccess[typeof(Registration).Name] = CheckRegistrationAccess;
            checkAccess[typeof(RegistrationAccount).Name] = CheckRegistrationAccountAccess;
            checkAccess[typeof(RegistrationDoc).Name] = CheckRegistrationDocAccess;
            checkAccess[typeof(Scan).Name] = CheckScanAccess;
            checkAccess[typeof(ServiceGroupSaldo).Name] = CheckServiceGroupSaldoAccess;
            checkAccess[typeof(User).Name] = CheckUserAccess;
#endif
        }

        private readonly string checkType;
        private readonly Dictionary<string, Func<object, object>> getValues = new();

        public UserHasAccessFilterAttribute(Type typeToAccess, string argument, string property = null)
        {
            this.checkType = typeToAccess.Name;
            getValues.Add(argument, property == null ?
                x => x :
                x => x.GetType().GetProperty(property).GetValue(x));
            Roles = Roles.Support;
        }

        public UserHasAccessFilterAttribute(string checkType, params string[] argumentsProperties)
        {
            this.checkType = checkType;
            int argumentsCount = argumentsProperties.Length / 2 + argumentsProperties.Length % 2;
            for (int i = 0; i < argumentsCount; i++)
            {
                var argument = argumentsProperties.ElementAt(i * 2);
                var property = argumentsProperties.ElementAtOrDefault(i * 2 + 1);
                getValues.Add(argument, property == null ?
                    x => x :
                    x => x.GetType().GetProperty(property).GetValue(x));
            }
            Roles = Roles.Support;
        }

        public Roles Roles
        {
            get => _Roles;
            set
            {
                _Roles = value;
                roles = _Roles.FlagsToStrings();
            }
        }
        private Roles _Roles;

        private string[] roles;

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.HttpContext.User.IsInRoles(roles))
            {
                bool hasAccess = false;
                try
                {
                    if (checkAccess.ContainsKey(checkType))
                    {
                        int userId = int.Parse(context.HttpContext.User.FindFirstValue(ClaimTypes.UserData));
                        var values = new List<object>();
                        foreach (var getValuesItem in getValues)
                        {
                            var argument = getValuesItem.Key;
                            var getValue = getValuesItem.Value;
                            object arg = context.ActionArguments[argument];
                            if (arg is not IEnumerable enumerableArg || arg is string)
                            {
                                values.Add(getValue(arg));
                            }
                            else
                            {
                                var enumerableValues = new List<object>();
                                foreach (var item in enumerableArg)
                                {
                                    enumerableValues.Add(getValue(item));
                                }
                                values.Add(enumerableValues);
                            }
                        }
                        var checks = new List<List<object>>
                        {
                            new List<object>()
                        };
                        foreach (var value in values)
                        {
                            if (value is not List<object> enumerableValues)
                            {
                                foreach (var checkValues in checks)
                                {
                                    checkValues.Add(value);
                                }
                            }
                            else
                            {
                                var newChecks = new List<List<object>>();
                                foreach (var checkValues in checks)
                                {
                                    foreach (var item in enumerableValues)
                                    {
                                        var newCheckValues = new List<object>(checkValues)
                                        {
                                            item
                                        };
                                        newChecks.Add(newCheckValues);
                                    }
                                }
                                checks = newChecks;
                            }
                        }
                        foreach (var checkValues in checks)
                        {
                            hasAccess = await checkAccess[checkType](context.HttpContext.User, userId, checkValues);
                            if (!hasAccess) break;
                        }
                    }
                    else
                    {
                        hasAccess = true;
                    }
                }
                catch
                {
                    hasAccess = false;
                }
                if (!hasAccess)
                {
                    context.Result = (context.Controller as ControllerBase).Forbid();
                    return;
                }
            }
            await next();
        }


    }
}
