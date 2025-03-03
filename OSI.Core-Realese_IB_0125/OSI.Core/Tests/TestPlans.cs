using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OSI.Core.Tests
{
    public class TestPlans
    {
        private string connectionString = "Host=10.1.1.125;Database=osi_billing;Username=postgres;Password=Aa222111";
        private int testOsiId = 97;

        public TestPlans()
        {
            OSIBillingDbContext.ConnectionString = connectionString;
        }

        [Fact]
        public async void TestFindFirstPlan()
        {
            using var db = OSIBillingDbContext.DbContext;
            var firstPlan = await db.PlanAccurals.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.OsiId == 97 && x.AccuralCompleted);
            Assert.Equal(new DateTime(2021, 11, 1), firstPlan.BeginDate);
            firstPlan = await db.PlanAccurals.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.OsiId == 36 && x.AccuralCompleted);
            Assert.Equal(new DateTime(2021, 07, 1), firstPlan.BeginDate);
            firstPlan = await db.PlanAccurals.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.OsiId == 96 && x.AccuralCompleted);
            Assert.Equal(new DateTime(2021, 10, 1), firstPlan.BeginDate);
        }
    }
}
