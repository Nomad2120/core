using ESoft.CommonLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OSI.Core.Logging;
using OSI.Core.Models.Db;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OSI.Core.Tests
{
    public class TestBaseRate
    {
        private readonly string connectionString = "Host=10.1.1.125;Database=osi_billing;Username=postgres;Password=Aa222111";
        private readonly IBaseRateSvc baseRateSvc;

        public TestBaseRate()
        {
            OSIBillingDbContext.ConnectionString = connectionString;
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IBaseRateSvc, BaseRateSvc>();
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            baseRateSvc = serviceProvider.GetService<IBaseRateSvc>();
        }

        [Fact]
        public async Task TestGetBaseRate()
        {
            var baseRate = await baseRateSvc.GetBaseRate(2023, 6);
            Assert.Equal(16.75m, baseRate);
            baseRate = await baseRateSvc.GetBaseRate(2023, 6);
            Assert.Equal(16.75m, baseRate);
            baseRate = await baseRateSvc.GetBaseRate(2023, 10);
            Assert.Equal(16.5m, baseRate);
            baseRate = await baseRateSvc.GetBaseRate(2023, 11);
            Assert.Equal(16m, baseRate);
        }
    }
}
