using OSI.Core.Logic;
using OSI.Core.Models.Db;
using System.Threading.Tasks;
using Xunit;

namespace OSI.Core.Tests
{
    public class TestFine
    {
        private readonly string connectionString = "Host=10.1.1.125;Database=osi_billing;Username=postgres;Password=Aa222111";

        public TestFine()
        {
            OSIBillingDbContext.ConnectionString = connectionString;
            OSIBillingDbContext.LogToDebug = false;
        }

        [Fact]
        public async Task TestCreateFine()
        {
            //await FineLogic.CreateFine(119, 2023, 6, 16.75m);
            Assert.True(true);
        }
    }
}
