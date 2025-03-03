using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface ITariffSvc : IModelService<OSIBillingDbContext, Tariff>
    {
        Task<decimal> GetTariffValueByRca(string rca);
    }

    public class TariffSvc : ModelService<OSIBillingDbContext, Tariff>, ITariffSvc
    {
        public const decimal DefaultTariff = 100m;

        private readonly IAddressRegistrySvc addressRegistrySvc;

        public TariffSvc(IAddressRegistrySvc addressRegistrySvc)
        {
            this.addressRegistrySvc = addressRegistrySvc;
        }

        public async Task<decimal> GetTariffValueByRca(string rca)
        {
            var region = await addressRegistrySvc.GetRegionByRca(rca);
            using var db = OSIBillingDbContext.DbContext;
            return (await db.Tariffs.OrderByDescending(t => t.Date).FirstOrDefaultAsync(t => t.AtsId == region.AtsId && t.Date <= DateTime.Today))?.Value ?? DefaultTariff;
        }
    }
}
