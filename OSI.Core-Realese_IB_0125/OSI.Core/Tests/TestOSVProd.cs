using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OSI.Core.Logic;
using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OSI.Core.Tests
{
    public class TestOSVProd
    {
        private readonly string connectionString = "Host=10.1.1.25;Database=osi_billing;Username=postgres;Password=Aa222111";        

        public TestOSVProd()
        {
            OSIBillingDbContext.ConnectionString = connectionString;
        }

        [Fact]
        public async Task TestOSVVariants()
        {
            Abonent abonent = new Abonent
            {
                Id = 7600,
                OsiId = 107 // Элит 13А
            };
            var saldoAllPeriod = await OSVLogic.GetEndSaldoOnAllPeriodByAbonent(abonent);
            string json = JsonConvert.SerializeObject(saldoAllPeriod);

            // проверяем сальдо и кол-во услуг отдельно за месяц
            var monthSaldo = saldoAllPeriod.FirstOrDefault(x => x.Period == new DateTime(2022, 5, 1));
            Assert.Equal("Май 2022", monthSaldo.PeriodDescription);

            var ondate = await OSVLogic.GetEndSaldoOnDateByAbonent(new DateTime(2022, 5, 1), abonent);
            Assert.Equal(ondate.TotalDebt, monthSaldo.Services.Sum(x => x.Begin));
        }        
    }
}
