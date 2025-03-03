using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using OSI.Core.Logic;
using OSI.Core.Services;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System;
using OSI.Core.Models.Db;
using Microsoft.EntityFrameworkCore;

namespace OSI.Core.Jobs
{
    public class CreateAccountReportsJob : IInvocable
    {
        private const int MaxParallelTasks = 1;

        private readonly ILogger<CreateAccountReportsJob> logger;
        private readonly IAccountReportSvc accountReportSvc;

        public CreateAccountReportsJob(ILogger<CreateAccountReportsJob> logger, IAccountReportSvc accountReportSvc)
        {
            this.logger = logger;
            this.accountReportSvc = accountReportSvc;
        }

        public async Task Invoke()
        {
            logger.LogInformation("Begin");
            try
            {
                var currentMonth = DateTime.Today.AddDays(1 - DateTime.Today.Day);
                using var db = OSIBillingDbContext.DbContext;
                var osiIds = await db.Osies.Where(o => o.IsLaunched).Select(o => o.Id).ToListAsync();
                if (osiIds.Any())
                {
                    logger.LogInformation("Got osiIds");
                    var queue = new ConcurrentQueue<int>(osiIds);
                    var tasks = Enumerable.Range(0, MaxParallelTasks).Select(_ => Task.Run(async () =>
                    {
                        while (queue.TryDequeue(out var osiId))
                        {
                            logger.LogInformation("Osi {osiId} begin", osiId);
                            try
                            {
                                await accountReportSvc.CreateAccountReport(new() { OsiId = osiId, Period = currentMonth });
                            }
                            catch (Exception ex)
                            {
                                logger.LogInformation("Osi {osiId} error: {error}", osiId, ex.Message);
                                logger.LogError(ex, "Osi {osiId} error", osiId);
                            }
                            logger.LogInformation("Osi {osiId} end", osiId);
                        }
                    }));
                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation("Error: {error}", ex.Message);
                logger.LogError(ex, "Error");
            }
            logger.LogInformation("End");
        }
    }
}
