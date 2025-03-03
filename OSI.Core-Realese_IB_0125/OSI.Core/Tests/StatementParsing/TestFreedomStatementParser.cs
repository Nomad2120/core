using Blazorise.Extensions;
using OSI.Core.Logic.BankStatementParsing;
using OSI.Core.Models.Enums;
using System;
using System.IO;
using Xunit;

namespace OSI.Core.Tests.StatementParsing
{
    public class TestFreedomStatementParser
    {
        [Fact]
        public void TestFreedomFormat()
        {
            IStatementParser freedomParsing = new FreedomBankStatementParser();
            byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\FreedomFiles\freedom.xls");
            bool rightFormat = freedomParsing.CheckFileFormat(data);
            Assert.True(rightFormat);
            data = File.ReadAllBytes(@"Tests\StatementParsing\HalykFiles\halyk_ansi.txt");
            rightFormat = freedomParsing.CheckFileFormat(data);
            Assert.False(rightFormat);
        }

        [Fact]
        public void TestFreedomParseData()
        {
            byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\FreedomFiles\freedom.xls");
            var freedomParsing = new FreedomBankStatementParser();
            var bs = freedomParsing.ParseData(data);
            Assert.Equal("KZ42551D128000023KZT", bs.Account);
            Assert.Equal(new DateTime(2024, 5, 1), bs.PeriodStart);
            Assert.Equal(new DateTime(2024, 5, 31), bs.PeriodEnd);
            Assert.Equal(570709.27m, bs.Begin);
            Assert.Equal(0.00m, bs.Debet);
            Assert.Equal(189105.89m, bs.Kredit);
            Assert.Equal(759815.16m, bs.End);
            
            Assert.Equal(21, bs.Items.Count);

            int itemNum = 0;
            Assert.Equal(new DateTime(2024, 5, 2), bs.Items[itemNum].Dt.Date);
            Assert.Equal("Объединение собственников имущества  \"Манхэттен дом №11Г\"", bs.Items[itemNum].Receiver);
            Assert.Equal("211240026135", bs.Items[itemNum].ReceiverBin);
            Assert.Equal("Объединение собственников имущества  \"Манхэттен дом №11Г\"", bs.Items[itemNum].Sender);
            Assert.Equal("211240026135", bs.Items[itemNum].SenderBin);
            Assert.Equal(3261.08m, bs.Items[itemNum].Amount);
            Assert.Equal(OperationTypeCodes.KREDIT, bs.Items[itemNum].OperationTypeCode);

            itemNum = 3;
            Assert.Equal(new DateTime(2024, 5, 4), bs.Items[itemNum].Dt.Date);
            Assert.Equal("Объединение собственников имущества  \"Манхэттен дом №11Г\"", bs.Items[itemNum].Receiver);
            Assert.Equal("211240026135", bs.Items[itemNum].ReceiverBin);
            Assert.Equal("АО \"KASPI BANK\"", bs.Items[itemNum].Sender);
            Assert.Equal("971240001315", bs.Items[itemNum].SenderBin);
            Assert.Equal(1724.81m, bs.Items[itemNum].Amount);
            Assert.Equal(OperationTypeCodes.KREDIT, bs.Items[itemNum].OperationTypeCode);
        }
    }
}
