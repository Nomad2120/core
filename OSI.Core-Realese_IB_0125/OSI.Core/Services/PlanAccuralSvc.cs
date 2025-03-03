using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OSI.Core.Logic;
using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using OSI.Core.Models.Requests;
using OSI.Core.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IPlanAccuralSvc
    {
        Task CheckPlanAccuralById(int id);
        string GetExceptionMessage(Exception ex);
        Task<PlanAccural> GetPlanAccuralById(int id);
        Task<IEnumerable<PlanAccural>> GetPlanAccuralsByOsiId(int osiId);
        Task<IEnumerable<PlanAccuralsOsiResponse>> GetAllPlanAccuralsOnPeriod(DateTime sd, DateTime ed);
        Task AddOrUpdateModel(PlanAccural model);
        Task<IEnumerable<Act>> GetActsByPlanId(int planId);
        Task SetAccuralJobAtDay(int planId, int accuralDay);
    }

    public class PlanAccuralSvc : IPlanAccuralSvc
    {
        private readonly IServiceProvider serviceProvider;

        public PlanAccuralSvc(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public string GetExceptionMessage(Exception ex)
        {
            string message = ex.Message;
            if (ex is DbUpdateException)
            {
                if (ex.InnerException.Message.IndexOf("unq_plan_accurals_osi_date") > -1)
                    message = "На эту дату уже есть план начислений по данному ОСИ";
                else if (ex.InnerException.Message.IndexOf("unq_plan_accural_services_plan_osiservice") > -1)
                    message = "Данная услуга уже есть в этом плане начислений";
                else if (ex.InnerException.Message.IndexOf("unq_plan_accural_abonents_data_abonents") > -1)
                    message = "На данной услуге уже прописан этот абонент";
                else if (ex.InnerException.Message.IndexOf("fk_plan_accural_abonents_abonents") > -1)
                    message = "Абонент не найден";
            }
            return message;
        }

        public async Task<PlanAccural> GetPlanAccuralById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var planAccural = await db.PlanAccurals.Include(p => p.Osi).FirstOrDefaultAsync(a => a.Id == id);
            if (planAccural == null)
                throw new Exception("План начислений не найден");
            return planAccural;
        }

        public async Task CheckPlanAccuralById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (!await db.PlanAccurals.AnyAsync(o => o.Id == id))
                throw new Exception("План начислений не найден");
        }

        public async Task<IEnumerable<PlanAccural>> GetPlanAccuralsByOsiId(int osiId)
        {
            _ = await serviceProvider.GetRequiredService<IOsiSvc>().GetOsiById(osiId);
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.PlanAccurals.Where(a => a.OsiId == osiId).ToListAsync();
            return models;
        }

        public async Task<IEnumerable<PlanAccuralsOsiResponse>> GetAllPlanAccuralsOnPeriod(DateTime sd, DateTime ed)
        {
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.Osies
                .Include(o => o.PlanAccurals)
                .Where(o => o.IsLaunched)
                .Select(o => new PlanAccuralsOsiResponse
                {
                    Osi = o,
                    PlanAccurals = o.PlanAccurals.Where(p => p.BeginDate >= sd.Date && p.BeginDate < ed.Date.AddDays(1)).ToList()
                }).ToListAsync();
            return models;
        }

        public async Task AddOrUpdateModel(PlanAccural model)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (model.Id == default)
            {
                db.PlanAccurals.Add(model);
            }
            else
            {
                db.PlanAccurals.Update(model);
            }
            await db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Act>> GetActsByPlanId(int planId)
        {
            await CheckPlanAccuralById(planId);
            using var db = OSIBillingDbContext.DbContext;
            var acts = await db.Acts.Include(a => a.State).Where(a => a.PlanAccuralId == planId).ToListAsync();
            return acts;
        }

        public async Task SetAccuralJobAtDay(int planId, int accuralDay)
        {
            var plan = await GetPlanAccuralById(planId);
            if (accuralDay < 1 || accuralDay > 10)
                throw new Exception("Устанавливаемое число месяца должно быть в пределах от 1 до 10");

            if (!plan.AccuralCompleted)
            {
                if (DateTime.Today.Day < 10)
                {
                    if (accuralDay < DateTime.Today.AddDays(1).Day)
                        throw new Exception("Устанавливаемое число месяца должно быть не меньше сегодняшнего числа +1 день");
                }
                else throw new Exception("Текущее число месяца не позволяет сделать начисления автоматически. Обратитесь к администратору.");
            }

            plan.AccuralJobAtDay = accuralDay;
            await AddOrUpdateModel(plan);
        }
    }
}
