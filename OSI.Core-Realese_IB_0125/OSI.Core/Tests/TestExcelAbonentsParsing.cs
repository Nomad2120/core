using OSI.Core.Logic;
using OSI.Core.Models.Db;
using OSI.Core.Models.ExcelAbonents;
using System.IO;
using Xunit;

namespace OSI.Core.Tests
{
    public class TestExcelAbonentsParsing
    { 
        [Fact]
        public void TestReadExcelAndParseRows()
        {            
            var parser = new ExcelAbonentsLoader();
            var data = File.ReadAllBytes(@"f:\Downloads\шаблон eOSI — итог.xlsx");
            var result = parser.ReadExcelFile(data);
            Assert.Equal("", result.ErrorMessage);
            Assert.True(result.Success);
            Assert.Equal(8, result.Abonents.Count);

            Assert.Equal(71.02m, result.Abonents[0].Square);
            Assert.Equal(78.03m, result.Abonents[0].EffectiveSquare);

            Assert.Equal(74.03m, result.Abonents[1].Square);
            Assert.Equal(78.03m, result.Abonents[1].EffectiveSquare);

            Assert.Equal(44.321m, result.Abonents[2].Square);
            Assert.Equal(0, result.Abonents[2].EffectiveSquare);

            Assert.Equal(500.3m, result.Abonents[3].Debt);
            Assert.Equal(600.12m, result.Abonents[4].Debt);
            Assert.Equal(410.3456m, result.Abonents[5].Debt);
        }
    }
}
