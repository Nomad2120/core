using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OSI.Core.Models.Db;
using OSI.Core.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface ICatalogSvc
    {
        Task<IEnumerable<AccuralMethod>> GetAccuralMethods();
        Task<IEnumerable<AreaType>> GetAreaTypes();
        Task<IEnumerable<AccountType>> GetAccountTypes();
        Task CheckBankByBic(string bic);
        Task<IEnumerable<Bank>> GetBanks();
        Task<IEnumerable<DocType>> GetDocTypes();
        Task<IEnumerable<HouseState>> GetHouseStates();
        Task CheckKnpByCode(string code);
        Task<IEnumerable<Knp>> GetKnps();
        Task<IEnumerable<ServiceGroup>> GetServiceGroups();
        Task<IEnumerable<ServiceCompany>> GetServiceCompanies();
        Task<IEnumerable<UnionType>> GetUnionTypes();
    }

    public class CatalogSvc : ICatalogSvc
    {
        public CatalogSvc()
        {
        }

        public async Task<IEnumerable<AreaType>> GetAreaTypes()
        {
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.AreaTypes.ToListAsync();
            return models;
        }

        public async Task<IEnumerable<AccountType>> GetAccountTypes()
        {
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.AccountTypes.ToListAsync();
            return models;
        }

        public async Task CheckBankByBic(string bic)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (!await db.Banks.AnyAsync(o => o.Bic == bic))
                throw new Exception("Банк не найден");
        }

        public async Task<IEnumerable<Bank>> GetBanks()
        {
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.Banks.OrderBy(b => b.Identifier == null).ThenBy(b => b.Bic).ToListAsync();
            return models;
        }

        public async Task<IEnumerable<DocType>> GetDocTypes()
        {
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.DocTypes.ToListAsync();
            return models;
        }

        public async Task<IEnumerable<HouseState>> GetHouseStates()
        {
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.HouseStates.ToListAsync();
            return models;
        }
        public async Task CheckKnpByCode(string code)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (!await db.Knps.AnyAsync(o => o.Code == code))
                throw new Exception("КНП не найден");
        }

        public async Task<IEnumerable<Knp>> GetKnps()
        {
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.Knps.ToListAsync();
            return models;
        }

        public async Task<IEnumerable<ServiceCompany>> GetServiceCompanies()
        {
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.ServiceCompanies.ToListAsync();
            return models;
        }

        public async Task<IEnumerable<ServiceGroup>> GetServiceGroups()
        {
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.ServiceGroups
                .Include(g => g.AccountType)
                .OrderBy(g => g.Id).ToListAsync();
            return models;
        }

        public async Task<IEnumerable<AccuralMethod>> GetAccuralMethods()
        {
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.AccuralMethods.ToListAsync();
            return models;
        }

        public async Task<IEnumerable<UnionType>> GetUnionTypes()
        {
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.UnionTypes.OrderBy(a => a.Id).ToListAsync();
            return models;
        }
    }
}
