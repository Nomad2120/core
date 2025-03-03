using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Logic
{
    public static class OsiBillingServiceLogic
    {
        public static async Task<OsiService> CreateOsiBillingService(OSIBillingDbContext db, int osiId, decimal tariff)
        {
            var abonents = await db.Abonents.Where(a => a.OsiId == osiId && !a.External).ToListAsync();
            var accuralMethod = await db.AccuralMethods.FirstOrDefaultAsync(a => a.Code == "FIX_SUM_FLAT");
            var serviceGroupId = await db.ServiceGroups.FirstOrDefaultAsync(a => a.Code == "TO");
            var osiService = new OsiService
            {
                OsiId = osiId,
                IsActive = true,
                IsOsibilling = true,
                NameRu = "Услуга Osi Billing",
                NameKz = "Услуга Osi Billing",
                ServiceGroupId = serviceGroupId.Id,
            };
            db.OsiServices.Add(osiService);
            await db.SaveChangesAsync();

            // сумма
            await PeriodicDataLogic.SaveOsiServiceAmount(db, new OsiServiceAmount
            {
                Amount = tariff,
                Dt = DateTime.Today,
                OsiId = osiId,
                OsiServiceId = osiService.Id,
                AccuralMethodId = accuralMethod.Id,
                Note = "добавление услуги"
            }, true);

            return osiService;
        }

        public static async Task SetStateOsiBillingService(OSIBillingDbContext db, OsiService osiBillingService, bool state)
        {
            osiBillingService.IsActive = state;
            db.OsiServices.Update(osiBillingService);
            var abonents = await db.Abonents.Where(a => a.OsiId == osiBillingService.OsiId && !a.External).ToListAsync();
            foreach (var abonent in abonents)
            {
                var connectedService = new ConnectedService
                {
                    AbonentId = abonent.Id,
                    OsiId = osiBillingService.OsiId,
                    OsiServiceId = osiBillingService.Id,
                    Dt = DateTime.Today,
                    IsActive = state
                };
                await PeriodicDataLogic.SaveConnectedService(db, connectedService);
            }
        }
    }
}
