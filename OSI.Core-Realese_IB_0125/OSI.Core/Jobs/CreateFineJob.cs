using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using OSI.Core.Logic;
using OSI.Core.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OSI.Core.Jobs
{
    public class CreateFineJob : IInvocable
    {
        private const int MaxParallelTasks = 4;

        private readonly ILogger<CreateFineJob> logger;
        private readonly IBaseRateSvc baseRateSvc;

        public CreateFineJob(ILogger<CreateFineJob> logger, IBaseRateSvc baseRateSvc)
        {
            this.logger = logger;
            this.baseRateSvc = baseRateSvc;
        }

        public async Task Invoke()
        {
            logger.LogInformation("Begin");
            try
            {
                var currentMonth = DateTime.Today.AddDays(1 - DateTime.Today.Day);
                var osiIds = await FineLogic.GetOsiIdsToCreateFine();
                if (osiIds.Any())
                {
                    logger.LogInformation("Got osiIds");
                    var baseRate = await baseRateSvc.GetBaseRate(currentMonth.Year, currentMonth.Month);
                    logger.LogInformation("Base rate: {baseRate}%", baseRate);
                    var queue = new ConcurrentQueue<int>(osiIds);
                    var tasks = Enumerable.Range(0, MaxParallelTasks).Select(_ => Task.Run(async () =>
                    {
                        while (queue.TryDequeue(out var osiId))
                        {
                            logger.LogInformation("Osi {osiId} begin", osiId);
                            try
                            {
                                await FineLogic.CreateFine(osiId, currentMonth.Year, currentMonth.Month, baseRate);
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
