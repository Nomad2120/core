using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IBaseRateSvc
    {
        Task<decimal> GetBaseRate(int year, int month);
    }

    public class BaseRateSvc : IBaseRateSvc
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);

        public async Task<decimal> GetBaseRate(int year, int month)
        {
            var period = new DateTime(year, month, 1);
            if (period > DateTime.Today.AddDays(1 - DateTime.Today.Day))
                throw new Exception("Нельзя получить ставку на будущее время");
            await semaphore.WaitAsync();
            try
            {
                using var db = OSIBillingDbContext.DbContext;
                var baseRate = db.BaseRates
                    .OrderByDescending(x => x.Period)
                    .FirstOrDefault(x => x.Period != DateTime.MinValue && x.Period <= period)
                    ?? throw new Exception($"Нет данных о базовой ставке на {period:MMMM yyyy}");
                if (baseRate.Period < period)
                {
                    db.BaseRates.Add(new()
                    {
                        Period = period,
                        Value = baseRate.Value,
                    });
                    await db.SaveChangesAsync();
                }
                return baseRate.Value;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
