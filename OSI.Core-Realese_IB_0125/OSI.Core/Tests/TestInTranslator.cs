using OSI.Core.Models.Db;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Xunit;
using System.Linq;
using ESoft.CommonLibrary;
using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Enums;

namespace OSI.Core.Tests
{
    public class TestInTranslator
    {
        private readonly string connectionString = "Host=10.1.1.125;Database=osi_billing;Username=postgres;Password=Aa222111";

        public TestInTranslator()
        {
            OSIBillingDbContext.ConnectionString = connectionString;
        }

        [Fact]
        public async Task TestInTranslation()
        {
            using var db = OSIBillingDbContext.DbContext;
            var contracts = await db.Contracts
                .Where(c => c.BankCode.In("CASP", "4254"))
                .ToListAsync();
            Assert.Equal(2, contracts.Count);
            var codes = new List<string>() { "CASP", "4254", "OSI" };
            contracts = await db.Contracts
                .Where(c => c.BankCode.In(codes))
                .ToListAsync();
            Assert.Equal(3, contracts.Count);
            var transactions = await db.Transactions
                .Where(t => t.TransactionType.In(TransactionTypeCodes.ACC, TransactionTypeCodes.FIX))
                .Take(4)
                .ToListAsync();
            Assert.Equal(4, transactions.Count);
        }
    }
}
