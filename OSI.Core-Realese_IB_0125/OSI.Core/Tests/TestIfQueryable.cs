using Microsoft.EntityFrameworkCore;
using OSI.Core.Helpers;
using OSI.Core.Models.Db;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OSI.Core.Tests
{
    public class TestIfQueryable
    {
        private readonly string connectionString = "Host=10.1.1.25;Database=osi_billing;Username=postgres;Password=Aa222111";

        public TestIfQueryable()
        {
            OSIBillingDbContext.ConnectionString = connectionString;
        }

        [Fact]
        public async Task SuccessIfTrueEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonent = await db.Abonents
                .If(true)
                .Where(a => a.Id == 727)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            Assert.Equal(727, abonent.Id);
        }

        [Fact]
        public async Task SuccessIfFalseEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonent = await db.Abonents
                .If(false)
                .Where(a => a.Id == 727)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            Assert.NotEqual(727, abonent.Id);
        }

        [Fact]
        public async Task SuccessIfTrueElseEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonent = await db.Abonents
                .If(true)
                .Where(a => a.Id == 727)
                .Else()
                .Where(a => a.Id == 728)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            Assert.Equal(727, abonent.Id);
        }

        [Fact]
        public async Task SuccessIfFalseElseEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonent = await db.Abonents
                .If(false)
                .Where(a => a.Id == 727)
                .Else()
                .Where(a => a.Id == 728)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            Assert.Equal(728, abonent.Id);
        }

        [Fact]
        public async Task SuccessIfTrueElseIfTrueEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonent = await db.Abonents
                .If(true)
                .Where(a => a.Id == 727)
                .ElseIf(true)
                .Where(a => a.Id == 728)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            Assert.Equal(727, abonent.Id);
        }

        [Fact]
        public async Task SuccessIfTrueElseIfFalseEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonent = await db.Abonents
                .If(true)
                .Where(a => a.Id == 727)
                .ElseIf(false)
                .Where(a => a.Id == 728)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            Assert.Equal(727, abonent.Id);
        }

        [Fact]
        public async Task SuccessIfFalseElseIfTrueEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonent = await db.Abonents
                .If(false)
                .Where(a => a.Id == 727)
                .ElseIf(true)
                .Where(a => a.Id == 728)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            Assert.Equal(728, abonent.Id);
        }

        [Fact]
        public async Task SuccessIfFalseElseIfFalseEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonent = await db.Abonents
                .If(false)
                .Where(a => a.Id == 727)
                .ElseIf(false)
                .Where(a => a.Id == 728)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            Assert.NotEqual(727, abonent.Id);
            Assert.NotEqual(728, abonent.Id);
        }

        [Fact]
        public async Task SuccessIfTrueElseIfTrueElseEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonent = await db.Abonents
                .If(true)
                .Where(a => a.Id == 727)
                .ElseIf(true)
                .Where(a => a.Id == 728)
                .Else()
                .Where(a => a.Id == 729)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            Assert.Equal(727, abonent.Id);
        }

        [Fact]
        public async Task SuccessIfTrueElseIfFalseElseEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonent = await db.Abonents
                .If(true)
                .Where(a => a.Id == 727)
                .ElseIf(false)
                .Where(a => a.Id == 728)
                .Else()
                .Where(a => a.Id == 729)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            Assert.Equal(727, abonent.Id);
        }

        [Fact]
        public async Task SuccessIfFalseElseIfTrueElseEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonent = await db.Abonents
                .If(false)
                .Where(a => a.Id == 727)
                .ElseIf(true)
                .Where(a => a.Id == 728)
                .Else()
                .Where(a => a.Id == 729)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            Assert.Equal(728, abonent.Id);
        }

        [Fact]
        public async Task SuccessIfFalseElseIfFalseElseEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonent = await db.Abonents
                .If(false)
                .Where(a => a.Id == 727)
                .ElseIf(false)
                .Where(a => a.Id == 728)
                .Else()
                .Where(a => a.Id == 729)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();
            Assert.Equal(729, abonent.Id);
        }

        [Fact]
        public async Task ErrorIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var ex = await Assert.ThrowsAsync<NotSupportedException>(() => db.Abonents
                .If(true)
                .Where(a => a.Id == 727)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync());
            Assert.Equal("Not supported query execution in If/ElseIf/Else", ex.Message);
        }

        [Fact]
        public async Task ErrorIfElse()
        {
            using var db = OSIBillingDbContext.DbContext;
            var ex = await Assert.ThrowsAsync<NotSupportedException>(() => db.Abonents
                .If(true)
                .Where(a => a.Id == 727)
                .Else()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync());
            Assert.Equal("Not supported query execution in If/ElseIf/Else", ex.Message);
        }

        [Fact]
        public async Task ErrorIfElseIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var ex = await Assert.ThrowsAsync<NotSupportedException>(() => db.Abonents
                .If(true)
                .Where(a => a.Id == 727)
                .ElseIf(true)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync());
            Assert.Equal("Not supported query execution in If/ElseIf/Else", ex.Message);
        }

        [Fact]
        public async Task ErrorIfElseIfElse()
        {
            using var db = OSIBillingDbContext.DbContext;
            var ex = await Assert.ThrowsAsync<NotSupportedException>(() => db.Abonents
                .If(true)
                .Where(a => a.Id == 727)
                .ElseIf(true)
                .Where(a => a.Id == 728)
                .Else()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync());
            Assert.Equal("Not supported query execution in If/ElseIf/Else", ex.Message);
        }

        [Fact]
        public async Task ErrorEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.Abonents
                .Where(a => a.Id == 727)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync());
            Assert.Equal("If call must be done before", ex.Message);
        }

        [Fact]
        public async Task ErrorElseIfEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.Abonents
                .ElseIf(true)
                .Where(a => a.Id == 727)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync());
            Assert.Equal("If call must be done before", ex.Message);
        }

        [Fact]
        public async Task ErrorElseEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.Abonents
                .Else()
                .Where(a => a.Id == 727)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync());
            Assert.Equal("If call must be done before", ex.Message);
        }

        [Fact]
        public async Task ErrorIfIfEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.Abonents
                .If(true)
                .Where(a => a.Id == 727)
                .If(true)
                .Where(a => a.Id == 729)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync());
            Assert.Equal("If has already been called", ex.Message);
        }

        [Fact]
        public async Task ErrorIfElseElseIfEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.Abonents
                .If(true)
                .Where(a => a.Id == 727)
                .Else()
                .Where(a => a.Id == 728)
                .ElseIf(true)
                .Where(a => a.Id == 729)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync());
            Assert.Equal("Else has already been called", ex.Message);
        }

        [Fact]
        public async Task ErrorIfElseElseEndIf()
        {
            using var db = OSIBillingDbContext.DbContext;
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.Abonents
                .If(true)
                .Where(a => a.Id == 727)
                .Else()
                .Where(a => a.Id == 728)
                .Else()
                .Where(a => a.Id == 729)
                .EndIf()
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync());
            Assert.Equal("Else has already been called", ex.Message);
        }
    }
}
