using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Logic
{
    public static class PlanAccuralLogic
    {
        public static async Task<PlanAccural> GetLastPlanAccuralByOsiIdOrCreateNew(int osiId)
        {
            using var db = OSIBillingDbContext.DbContext;
            var lastPlanDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); //(await db.SystemInformations.FirstOrDefaultAsync(p => p.Code == "CURRENT_PLAN_DATE")).DateValue;
            var plan = await db.PlanAccurals.OrderBy(p => p.Id).LastOrDefaultAsync(a => a.OsiId == osiId);
            if (plan == null || plan.BeginDate < lastPlanDate)
            {
                plan = await CopyLastPlanOrCreateNew(osiId, lastPlanDate/* ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)*/);
            }
            return plan;
        }

        public static async Task<PlanAccural> CopyLastPlanOrCreateNew(int osiId, DateTime planDate)
        {
            using var db = OSIBillingDbContext.DbContext;
            PlanAccural newPlan = new PlanAccural();
            //  найдем предыдущий план
            PlanAccural lastPlan = await db.PlanAccurals.OrderBy(p => p.Id).LastOrDefaultAsync(a => a.OsiId == osiId);
            newPlan.OsiId = osiId;
            newPlan.UssikingIncluded = lastPlan?.UssikingIncluded ?? true;
            newPlan.AccuralCompleted = false;
            newPlan.Tariff = lastPlan?.Tariff ?? TariffSvc.DefaultTariff; // тариф сейчас не важен, т.к. всё равно будет вычисляться в момент начислений
            newPlan.BeginDate = planDate;
            newPlan.AccuralJobAtDay = lastPlan?.AccuralJobAtDay ?? 3; // OSI-432: по-умолчанию это 3 день
            db.PlanAccurals.Add(newPlan);
            await db.SaveChangesAsync();

            return newPlan;
        }

        public static async Task<PlanAccural> GetPlanAccuralById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var planAccural = await db.PlanAccurals.Include(p => p.Osi).FirstOrDefaultAsync(a => a.Id == id);
            if (planAccural == null)
                throw new Exception("План начислений не найден");
            return planAccural;
        }
                
        public static async Task SetUssikingIncluded(int id, bool value)
        {
            PlanAccural planAccural = await GetPlanAccuralById(id);
            using var db = OSIBillingDbContext.DbContext;
            using var dbTransaction = await db.Database.BeginTransactionAsync();
            try
            {
                planAccural.UssikingIncluded = value;
                db.PlanAccurals.Update(planAccural);

                var osiBillingService = await db.OsiServices.FirstOrDefaultAsync(o => o.IsOsibilling && o.OsiId == planAccural.OsiId);

                // Не забываем, что тут действует обратная логика - при включенной галочке услуги оси биллинг не должно быть, и наоборот
                if (!value)
                {
                    if (osiBillingService == null)
                    {
                        var tariff = await OsiTariffLogic.GetOsiTariffValueByDate(planAccural.OsiId, DateTime.Today);
                        osiBillingService = await OsiBillingServiceLogic.CreateOsiBillingService(db, planAccural.OsiId, tariff);
                    }
                }

                if (osiBillingService != null)
                {
                    await OsiBillingServiceLogic.SetStateOsiBillingService(db, osiBillingService, !value);
                }

                await db.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }
}
