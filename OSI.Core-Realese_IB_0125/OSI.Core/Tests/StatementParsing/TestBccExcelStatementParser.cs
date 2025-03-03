using Blazorise.Extensions;
using OSI.Core.Logic.BankStatementParsing;
using OSI.Core.Models.Enums;
using System;
using System.IO;
using Xunit;

namespace OSI.Core.Tests.StatementParsing
{
    public class TestBccExcelStatementParser
    {
        [Fact]
        public void TestBccFormat()
        {
            IStatementParser bccParsing = new BccBankExcelStatementParser();
            byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\BccFiles\bcc.xls");
            bool rightFormat = bccParsing.CheckFileFormat(data);
            Assert.True(rightFormat);
            data = File.ReadAllBytes(@"Tests\StatementParsing\HalykFiles\halyk_ansi.txt");
            rightFormat = bccParsing.CheckFileFormat(data);
            Assert.False(rightFormat);
        }

        [Fact]
        public void TestBccParseData()
        {
            byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\BccFiles\bcc.xls");
            var bccParsing = new BccBankExcelStatementParser();
            var bs = bccParsing.ParseData(data);
            Assert.Equal("KZ728562203118191239", bs.Account);
            Assert.Equal(new DateTime(2023, 6, 1), bs.PeriodStart);
            Assert.Equal(new DateTime(2023, 6, 30), bs.PeriodEnd);
            Assert.Equal(100, bs.Begin);
            Assert.Equal(963630.75m, bs.Debet);
            Assert.Equal(948630.75m, bs.Kredit);
            Assert.Equal(15100, bs.End);
            
            Assert.Equal(11, bs.Items.Count);

            int itemNum = 0;
            Assert.Equal(new DateTime(2023, 6, 9), bs.Items[itemNum].Dt.Date);
            Assert.Equal("ИП Касимов", bs.Items[itemNum].Receiver);
            Assert.Equal("900828350333", bs.Items[itemNum].ReceiverBin);
            Assert.Equal("Объединение собственников имущества \"Жилой комплекс \"Юнис Сити\"", bs.Items[itemNum].Sender);
            Assert.Equal("181140004063", bs.Items[itemNum].SenderBin);
            Assert.Equal(343000, bs.Items[itemNum].Amount);
            Assert.Equal(OperationTypeCodes.DEBET, bs.Items[itemNum].OperationTypeCode);

            itemNum = 4;
            Assert.Equal(new DateTime(2023, 6, 14), bs.Items[itemNum].Dt.Date);
            Assert.Equal("ОСИ \"Жилой комплекс \"Юнис Сити\"", bs.Items[itemNum].Sender);
            Assert.Equal("Объединение собственников имущества \"Жилой комплекс \"Юнис Сити\"", bs.Items[itemNum].Receiver);
            Assert.Equal("181140004063", bs.Items[itemNum].ReceiverBin);
            Assert.Equal("181140004063", bs.Items[itemNum].SenderBin);
            Assert.Equal(40000, bs.Items[itemNum].Amount);
            Assert.Equal(OperationTypeCodes.KREDIT, bs.Items[itemNum].OperationTypeCode);
        }

        //[Fact]
        //public void TestBccParseData2()
        //{
        //    byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\BccFiles\1768.xls");
        //    var bccParsing = new BccBankStatementParser();
        //    var bs = bccParsing.ParseData(data);
        //    Assert.Equal("KZ718562215131181768", bs.Account);
        //    Assert.Equal(new DateTime(2024, 2, 1), bs.PeriodStart);
        //    Assert.Equal(new DateTime(2024, 2, 29), bs.PeriodEnd);
        //    Assert.Equal(893843.76m, bs.Begin);
        //    Assert.Equal(371935.98m, bs.Debet);
        //    Assert.Equal(0, bs.Kredit);
        //    Assert.Equal(1265779.74m, bs.End);

        //    Assert.Equal(34, bs.Items.Count);

        //    int itemNum = 0;
        //    Assert.Equal(new DateTime(2024, 2, 1), bs.Items[itemNum].Dt.Date);
        //    Assert.Equal("Объединение собственников имущества \"Жилой комплекс \"Юнис Сити\"", bs.Items[itemNum].Receiver);
        //    Assert.Equal("181140004063", bs.Items[itemNum].ReceiverBin);
        //    Assert.Equal("АО \"KASPI BANK\"", bs.Items[itemNum].Sender);
        //    Assert.Equal("971240001315", bs.Items[itemNum].SenderBin);
        //    Assert.Equal(2830.72m, bs.Items[itemNum].Amount);
        //    Assert.Equal(OperationTypeCodes.KREDIT, bs.Items[itemNum].OperationTypeCode);
        //}
    }
}
