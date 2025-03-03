using Microsoft.EntityFrameworkCore;
using OSI.Core.Logic;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Responses;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IBuhSvc
    {
        Task<string> CreateAccurals(int year, int month, bool recreate, bool allOsi);
        Task<string> CreateAccurals(bool recreate);
        Task<string> CreateActs(int year, int month, bool onlyIfNotExists = false);
        Task<string> CreateEsfs(int year, int month);

        //Task<string> CreateNewPlans();
        string GetExceptionMessage(Exception ex);
    }

    public class BuhSvc : IBuhSvc
    {
        private readonly ITransactionSvc transactionSvc;
        private readonly IActSvc actSvc;

        public BuhSvc(ITransactionSvc transactionSvc, IActSvc actSvc)
        {
            this.transactionSvc = transactionSvc;
            this.actSvc = actSvc;
        }

        // делаем начисления за указанный период, если планов начислений не было - не создаются        
        public async Task<string> CreateAccurals(int year, int month, bool recreate, bool allOsi = false)
        {
            using var db = OSIBillingDbContext.DbContext;

            var plans = await db.PlanAccurals
                .Include(p => p.Osi)
                .Where(p => p.BeginDate == new DateTime(year, month, 1))
                .Where(p => recreate || !p.AccuralCompleted)
                .Where(p => allOsi || p.Osi.IsLaunched)
                .ToArrayAsync();

            string errors = "";
            foreach (var plan in plans)
            {
                string osiName = plan.Osi.Name;
                try
                {
                    if (recreate || !plan.AccuralCompleted)
                    {
                        await transactionSvc.CreateAccuralsByPlanId(plan.Id, true);
                    }
                }
                catch (Exception ex)
                {
                    errors += osiName + ", planId=" + plan.Id + ": " + ex.Message + Environment.NewLine;
                }
            }
            if (string.IsNullOrEmpty(errors))
                errors = "OK";
            return errors;
        }

        // делаем начисления в текущем периоде, если планов начислений не было - создаются
        public async Task<string> CreateAccurals(bool recreate)
        {
            using var db = OSIBillingDbContext.DbContext;
            var osies = await db.Osies.Where(o => o.IsLaunched).ToListAsync();
            string errors = "";
            foreach (var osi in osies)
            {
                string osiName = osi.Name;
                int planId = 0;
                try
                {
                    var plan = await PlanAccuralLogic.GetLastPlanAccuralByOsiIdOrCreateNew(osi.Id);
                    planId = plan.Id;
                    if (recreate || !plan.AccuralCompleted)
                    {
                        await transactionSvc.CreateAccuralsByPlanId(plan.Id, true);
                    }
                }
                catch (Exception ex)
                {
                    errors += osiName + ", planId=" + planId + ": " + ex.Message + Environment.NewLine;
                }
            }
            if (string.IsNullOrEmpty(errors))
                errors = "OK";
            return errors;
        }

        // создаем акты за указанную дату
        public async Task<string> CreateActs(int year, int month, bool onlyIfNotExists = false)
        {
            string errors = "";
            try
            {
                if (year == DateTime.Today.Year && month == DateTime.Today.Month)
                    throw new Exception("Нельзя создавать акты текущим месяцем!");

                using var db = OSIBillingDbContext.DbContext;
                var plans = await db.PlanAccurals.Include(p => p.Osi).Where(p => p.Osi.IsLaunched && p.AccuralCompleted && p.BeginDate == new DateTime(year, month, 1)).ToArrayAsync();
                foreach (var plan in plans)
                {
                    try
                    {
                        _ = await actSvc.CreateActByPlanAccural(plan, onlyIfNotExists);
                    }
                    catch (Exception ex)
                    {
                        errors += plan.Osi.Name + ", planId=" + plan.Id + ": " + ex.Message + Environment.NewLine;
                    }
                }
                if (string.IsNullOrEmpty(errors))
                    errors = "OK";
            }
            catch (Exception ex)
            {
                errors = ex.Message;
            }
            return errors;
        }

        // создаем ЭСФ по актам за указанный месяц
        public async Task<string> CreateEsfs(int year, int month)
        {
            string errors = "";
            try
            {
                using var db = OSIBillingDbContext.DbContext;
                var acts = await db.Acts
                    .Include(a => a.Osi)
                    .Include(a => a.PlanAccural)
                    .Where(a => a.ActPeriod.Year == year)
                    .Where(a => a.ActPeriod.Month == month)
                    .ToArrayAsync();

                foreach (var act in acts)
                {
                    var apiResponse = new ApiResponse<EsfUploadResponse>();
                    try
                    {
                        apiResponse = await actSvc.CreateEsf(act);
                    }
                    catch (Exception ex)
                    {
                        apiResponse.FromEx(ex);
                    }
                    errors += act.Osi.Name + ", planId=" + act.PlanAccural.Id + ": " + apiResponse.Message + Environment.NewLine;
                }
                if (string.IsNullOrEmpty(errors))
                    errors = "OK";
            }
            catch (Exception ex)
            {
                errors += Environment.NewLine + ex.Message;
            }
            return errors;
        }

        public string GetExceptionMessage(Exception ex)
        {
            string message = ex.Message;
            return message;
        }
    }
}
