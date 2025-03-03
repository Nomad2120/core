using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using OSI.Core.Logic;
using OSI.Core.Services;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OSI.Core.Models.Db;
using Microsoft.EntityFrameworkCore;

namespace OSI.Core.Jobs
{
    public class CreateAccuralsJob : IInvocable
    {
        private const int MaxParallelTasks = 2;

        private readonly ILogger<CreateAccuralsJob> logger;
        private readonly ITransactionSvc transactionSvc;

        public CreateAccuralsJob(ILogger<CreateAccuralsJob> logger, ITransactionSvc transactionSvc)
        {
            this.logger = logger;
            this.transactionSvc = transactionSvc;
        }

        public async Task Invoke()
        {
            logger.LogInformation("Начало");
            try
            {
                var currentDate = DateTime.Today;
                using var db = OSIBillingDbContext.DbContext;
                var osies = await db.Osies.Where(o => o.IsLaunched).ToListAsync();
                var queue = new ConcurrentQueue<Osi>(osies);
                var tasks = Enumerable.Range(0, MaxParallelTasks).Select(_ => Task.Run(async () =>
                {
                    while (queue.TryDequeue(out var osi))
                    {
                        int planId = 0;
                        try
                        {
                            var plan = await PlanAccuralLogic.GetLastPlanAccuralByOsiIdOrCreateNew(osi.Id);
                            planId = plan.Id;
                            if (!plan.AccuralCompleted && plan.AccuralJobAtDay <= currentDate.Day)
                            {
                                logger.LogInformation("Osi {osi} создание начислений", osi.Name);
                                await transactionSvc.CreateAccuralsByPlanId(plan.Id, true);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogInformation("Osi {osi}, plan Id={planId} ошибка: {error}", osi.Name, planId, ex.ToString());
                            logger.LogError("Osi {osi}, plan Id={planId} ошибка: {error}", osi.Name, planId, ex.ToString());
                        }
                    }
                }));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                logger.LogInformation("Ошибка: {error}", ex.Message);
                logger.LogError(ex, "Ошибка");
            }
            logger.LogInformation("Конец");
        }
    }
}
