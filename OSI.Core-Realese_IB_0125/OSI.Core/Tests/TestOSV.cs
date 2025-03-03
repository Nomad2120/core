using Blazorise.Extensions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OSI.Core.Logic;
using OSI.Core.Models.Db;
using OSI.Core.Models.Responses;
using OSI.Core.Pages;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OSI.Core.Tests
{
    public class TestOSV
    {
        private readonly string connectionString = "Host=10.1.1.125;Database=osi_billing;Username=postgres;Password=Aa222111";
        private readonly int testOsiId = 39; // ОСИ Шумахер

        public TestOSV()
        {
            OSIBillingDbContext.ConnectionString = connectionString;
        }

        [Fact]
        public async void TestOsiShumakherDataIntegrity()
        {
            using var db = OSIBillingDbContext.DbContext;
            var transactions = await db.Transactions.Where(x => x.OsiId == testOsiId).ToListAsync();
            Assert.Equal(1115780.77m, transactions.Sum(x => x.Amount));
            Assert.Equal(147, transactions.Count);
        }

        [Fact]
        public async Task TestOSVVariants()
        {
            Abonent abonent = new Abonent
            {
                Id = 727,
                OsiId = testOsiId
            };
            var saldoAllPeriod = await OSVLogic.GetEndSaldoOnAllPeriodByAbonent(abonent);
            string json = JsonConvert.SerializeObject(saldoAllPeriod);

            // проверяем сальдо и кол-во услуг отдельно за месяц
            var monthSaldo = saldoAllPeriod.FirstOrDefault(x => x.Period == new DateTime(2021, 8, 1));
            Assert.Equal("Август 2021", monthSaldo.PeriodDescription);
            var ondate = await OSVLogic.GetEndSaldoOnDateByAbonent(new DateTime(2021, 8, 1), abonent);
            Assert.Equal(ondate.TotalDebt, monthSaldo.Services.Sum(x => x.Begin));
            ondate = await OSVLogic.GetEndSaldoOnDateByAbonent(new DateTime(2021, 8, 31), abonent);
            Assert.Equal(ondate.TotalDebt, monthSaldo.Services.Sum(x => x.End));
            Assert.Equal(ondate.Services.Count, monthSaldo.Services.Count);
            var onperiod = await OSVLogic.GetEndSaldoOnPeriodByAbonent(new DateTime(2021, 8, 1), new DateTime(2021, 8, 31), abonent);
            Assert.Equal(onperiod.Services.Sum(x => x.Begin), monthSaldo.Services.Sum(x => x.Begin));
            Assert.Equal(onperiod.Services.Sum(x => x.Debet), monthSaldo.Services.Sum(x => x.Debet));
            Assert.Equal(onperiod.Services.Sum(x => x.Kredit), monthSaldo.Services.Sum(x => x.Kredit));
            Assert.Equal(onperiod.Services.Sum(x => x.End), monthSaldo.Services.Sum(x => x.End));

            monthSaldo = saldoAllPeriod.FirstOrDefault(x => x.Period == new DateTime(2021, 9, 1));
            Assert.Equal("Сентябрь 2021", monthSaldo.PeriodDescription);
            Assert.Equal(ondate.TotalDebt, monthSaldo.Services.Sum(x => x.Begin));
            ondate = await OSVLogic.GetEndSaldoOnDateByAbonent(new DateTime(2021, 9, 30), abonent);
            Assert.Equal(ondate.TotalDebt, monthSaldo.Services.Sum(x => x.End));
            Assert.Equal(ondate.Services.Count, monthSaldo.Services.Count);
            onperiod = await OSVLogic.GetEndSaldoOnPeriodByAbonent(new DateTime(2021, 9, 1), new DateTime(2021, 9, 30), abonent);
            Assert.Equal(onperiod.Services.Sum(x => x.Begin), monthSaldo.Services.Sum(x => x.Begin));
            Assert.Equal(onperiod.Services.Sum(x => x.Debet), monthSaldo.Services.Sum(x => x.Debet));
            Assert.Equal(onperiod.Services.Sum(x => x.Kredit), monthSaldo.Services.Sum(x => x.Kredit));
            Assert.Equal(onperiod.Services.Sum(x => x.End), monthSaldo.Services.Sum(x => x.End));

            var osv = await OSVLogic.GetOSVOnDateByOsi(new DateTime(2021, 9, 1), new DateTime(2021, 9, 30), new Osi { Id = testOsiId });
            var osvAbonent = osv.Abonents.FirstOrDefault(x => x.AbonentId == abonent.Id);
            Assert.Equal(osvAbonent.Services.Sum(x => x.Begin), monthSaldo.Services.Sum(x => x.Begin));
            Assert.Equal(osvAbonent.Services.Sum(x => x.Debet), monthSaldo.Services.Sum(x => x.Debet));
            Assert.Equal(osvAbonent.Services.Sum(x => x.Kredit), monthSaldo.Services.Sum(x => x.Kredit));
            Assert.Equal(osvAbonent.Services.Sum(x => x.End), monthSaldo.Services.Sum(x => x.End));
        }

        [Fact]
        public async Task TestOsv97()
        {
            Abonent abonent = new Abonent
            {
                Id = 6781,
                OsiId = 97
            };
            var saldoAllPeriod = await OSVLogic.GetEndSaldoOnAllPeriodByAbonent(abonent);
            string json = JsonConvert.SerializeObject(saldoAllPeriod);
            var osv = await OSVLogic.GetOSVOnDateByOsi(new DateTime(2022, 3, 1), new DateTime(2022, 3, 24), new Osi { Id = 97 });
            var osvAbonent = osv.Abonents.FirstOrDefault(x => x.AbonentId == abonent.Id);
            json = JsonConvert.SerializeObject(osvAbonent);
            var ondate = await OSVLogic.GetEndSaldoOnDateByAbonent(new DateTime(2022, 3, 24), abonent);
            json = JsonConvert.SerializeObject(ondate);
            var monthSaldo = saldoAllPeriod.FirstOrDefault(x => x.Period == new DateTime(2022, 3, 1));
            Assert.Equal(ondate.Services.Count, monthSaldo.Services.Count);
            Assert.Equal(osvAbonent.Services.Count, monthSaldo.Services.Count);
        }

        [Fact]
        public async Task TestOsv97_2()
        {
            Abonent abonent = new Abonent
            {
                Id = 6782,
                OsiId = 97
            };
            var saldoAllPeriod = await OSVLogic.GetEndSaldoOnAllPeriodByAbonent(abonent);
            Assert.True(true);
        }

        [Fact]
        public async Task TestOsv369()
        {
            Abonent abonent = new Abonent
            {
                Id = 369,
                OsiId = 31
            };
            var saldoAllPeriod = await OSVLogic.GetEndSaldoOnAllPeriodByAbonent(abonent);
            // проверяем сальдо и кол-во услуг отдельно за месяц
            var monthSaldo = saldoAllPeriod.FirstOrDefault(x => x.Period == new DateTime(2021, 10, 1));
            var ondate = await OSVLogic.GetEndSaldoOnDateByAbonent(new DateTime(2021, 11, 1), abonent);
            Assert.Equal(ondate.TotalDebt, monthSaldo.Services.Sum(x => x.End));
        }

        //[Fact]
        public async Task TestGroupBy()
        {
            using var db = OSIBillingDbContext.DbContext;
            var osiConnectedServices = await db.ConnectedServices.Where(a => a.OsiId == 110).ToListAsync();
            var osiAbonents = await db.Abonents.Where(o => o.OsiId == 110).ToListAsync();
            var connectedServices = osiConnectedServices.Where(a => a.OsiServiceId == 351);
            //foreach (var abonent in osiAbonents)
            //{
            //    var abonentConnecting = osiConnectedServices.OrderByDescending(o => o.Dt).FirstOrDefault(a => a.AbonentId == abonent.Id && a.OsiServiceId == 351);
            //    if (abonentConnecting)

            //    activeAbonents.Add();
            //}
            //var groupsAbonents = osiConnectedServices
            //    .Where(a => a.OsiServiceId == 351 && a.IsActive)
            //    .GroupBy(a => a.Dt);
            //var lastActiveAbonents = groupsAbonents.FirstOrDefault().OrderByDescending(g => g.Dt);
            //var abonent = lastActiveAbonents.FirstOrDefault(a => a.AbonentId == 7772);
        }


        [Fact]
        public async void TestOSVForDebtors()
        {
            using var db = OSIBillingDbContext.DbContext;
            var startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endDate = startDate.AddMonths(1);
            var osv = await OSVLogic.GetOSVOnDateByOsi(startDate, endDate, new Osi { Id = 110 });
            var osvForDebtors = await OSVLogic.GetOSVOnDateByOsi(startDate, endDate, new Osi { Id = 110 }, forDebtors: true);
            if (DateTime.Today.Day >= 26)
            {
                Assert.Equal(
                    osv.Abonents.Sum(a=>a.Services.Sum(s=>s.End)), 
                    osvForDebtors.Abonents.Sum(a => a.Services.Sum(s => s.End)));
            }
            else
            {
                Assert.NotEqual(
                    osv.Abonents.Sum(a => a.Services.Sum(s => s.End)),
                    osvForDebtors.Abonents.Sum(a => a.Services.Sum(s => s.End)));
            }
        }
    }
}
