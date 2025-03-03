using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Logic
{
    public static class OsiTariffLogic
    {
        public static async Task<decimal> GetOsiTariffValueByDate(int osiId, DateTime date)
        {
            using var db = OSIBillingDbContext.DbContext;
            var osiTariffs = await db.OsiTariffs.Where(o => o.OsiId == osiId).ToListAsync();
            return GetOsiTariffValueFromListByDate(osiTariffs, date);
        }

        public static decimal GetOsiTariffValueFromListByDate(List<OsiTariff> osiTariffs, DateTime date)
        {
            var osiTariff = osiTariffs?.OrderByDescending(t => t.Dt).FirstOrDefault(t => t.Dt <= date) ?? null;
            return osiTariff?.Value ?? Services.TariffSvc.DefaultTariff;
        }
    }
}
