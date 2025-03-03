using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using OSI.Core.Logic.BankStatementParsing;
using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace OSI.Core.Tests.StatementParsing
{
    public class TestHalykStatementParser
    {
        [Fact]
        public void TestHalykFormat()
        {
            IStatementParser halykParsing = new HalykBankStatementParser();
            byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\HalykFiles\halyk_utf.txt");
            bool rightFormat = halykParsing.CheckFileFormat(data);
            Assert.True(rightFormat);
            data = File.ReadAllBytes(@"Tests\StatementParsing\HalykFiles\halyk_ansi.txt");
            rightFormat = halykParsing.CheckFileFormat(data);
            Assert.True(rightFormat);
            data = File.ReadAllBytes(@"Tests\StatementParsing\HalykFiles\halyk.xls");
            rightFormat = halykParsing.CheckFileFormat(data);
            Assert.False(rightFormat);
        }

        [Fact]
        public void TestHalykCalcTotalAmounts()
        {
            List<string> lines = new List<string>
            {
                "ДАТАНАЧАЛА=01.06.2023",
                "ДАТАКОНЦА=30.06.2023",
                "РАСЧСЧЕТ=KZ356017121000001925",
                "СЕКЦИЯРАСЧСЧЕТ",
                "ДАТАНАЧАЛА=01.06.2023",
                "ДАТАКОНЦА=01.06.2023",
                "РАСЧСЧЕТ=KZ356017121000001925",
                "НАЧАЛЬНЫЙОСТАТОК=5.55",
                "ВсегоПоступило=10",
                "ВсегоСписано=5",
                "КОНЕЧНЫЙОСТАТОК=10.55",
                "КОНЕЦРАСЧСЧЕТ",
                "СЕКЦИЯРАСЧСЧЕТ",
                "ДАТАНАЧАЛА=02.06.2023",
                "ДАТАКОНЦА=02.06.2023",
                "РАСЧСЧЕТ=KZ356017121000001925",
                "НАЧАЛЬНЫЙОСТАТОК=10.55",
                "ВсегоПоступило=50",
                "ВсегоСписано=20",
                "КОНЕЧНЫЙОСТАТОК=40.55",
            };
            var halykParsing = new HalykBankStatementParser();
            var totals = halykParsing.GetTotalAmounts(lines);
            Assert.Equal(5.55m, totals.Begin);
            Assert.Equal(60, totals.Debet);
            Assert.Equal(25, totals.Kredit);
            Assert.Equal(40.55m, totals.End);
            lines = new List<string>();
            totals = halykParsing.GetTotalAmounts(lines);
            Assert.Equal(0, totals.Begin);
            Assert.Equal(0, totals.Debet);
            Assert.Equal(0, totals.Kredit);
            Assert.Equal(0, totals.End);
        }

        [Fact]
        public void TestHalykGetPeriodBegin()
        {
            List<string> lines = new List<string>
            {
                "1CClientBankExchange",
                "ДАТАНАЧАЛА=01.06.2023",
                "ДАТАКОНЦА=30.06.2023",
            };
            var halykParsing = new HalykBankStatementParser();
            var dt = halykParsing.GetPeriodBegin(lines);
            Assert.Equal(new DateTime(2023, 6, 1), dt);
                        
            lines = new List<string>();
            Assert.ThrowsAny<Exception>(() =>
            {
                var dt = halykParsing.GetPeriodBegin(lines);
            });
            
            lines = new List<string>
            {
                "1CClientBankExchange",
                "НАЧАЛО=01.06.2023",
                "КОНЕЦ=30.06.2023",
            };
            Assert.ThrowsAny<Exception>(() =>
            {
                var dt = halykParsing.GetPeriodBegin(lines);
            });

            lines = new List<string>
            {
                "1CClientBankExchange",
                "ДАТАНАЧАЛА=ываыва"
            }; 
            Assert.ThrowsAny<Exception>(() =>
            {
                var dt = halykParsing.GetPeriodBegin(lines);
            });
        }

        [Fact]
        public void TestHalykGetPeriodEnd()
        {
            List<string> lines = new List<string>
            {
                "1CClientBankExchange",
                "ДАТАНАЧАЛА=01.06.2023",
                "ДАТАКОНЦА=30.06.2023",
            };
            var halykParsing = new HalykBankStatementParser();
            var dt = halykParsing.GetPeriodEnd(lines);
            Assert.Equal(new DateTime(2023, 6, 30), dt);

            lines = new List<string>();
            Assert.ThrowsAny<Exception>(() => { var dt = halykParsing.GetPeriodEnd(lines); });

            lines = new List<string>
            {
                "1CClientBankExchange",
                "НАЧАЛО=01.06.2023",
                "КОНЕЦ=30.06.2023",
            };
            Assert.ThrowsAny<Exception>(() => { var dt = halykParsing.GetPeriodEnd(lines);});

            lines = new List<string>
            {
                "1CClientBankExchange",
                "ДАТАНАЧАЛА=ываыва",
                "ДАТАКОНЦА=ываыва",
            };
            Assert.ThrowsAny<Exception>(() => { var dt = halykParsing.GetPeriodEnd(lines); });
        }

        [Fact]
        public void TestHalykGetAccount()
        {
            List<string> lines = new List<string>
            {
                "1CClientBankExchange",
                "ДАТАНАЧАЛА=01.06.2023",
                "ДАТАКОНЦА=30.06.2023",
                "РАСЧСЧЕТ=KZ356017121000001925",
            };
            var halykParsing = new HalykBankStatementParser();
            var account = halykParsing.GetAccount(lines);
            Assert.Equal("KZ356017121000001925", account);

            lines = new List<string>();
            Assert.ThrowsAny<Exception>(() => { var dt = halykParsing.GetAccount(lines); });

            lines = new List<string>
            {
                "", "", "", "ыва=йцу"
            };
            Assert.ThrowsAny<Exception>(() => { var dt = halykParsing.GetAccount(lines); });
        }

        [Fact]
        public void TestHalykParseAllData()
        {
            byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\HalykFiles\halyk_utf.txt");
            var halykParsing = new HalykBankStatementParser();
            var bs = halykParsing.ParseData(data);
            Assert.Equal("KZ356017121000001925", bs.Account);
            Assert.Equal(new DateTime(2023, 6, 1), bs.PeriodStart);
            Assert.Equal(new DateTime(2023, 6, 30), bs.PeriodEnd);
            Assert.Equal(386.43m, bs.Begin);
            Assert.Equal(11500, bs.Debet);
            Assert.Equal(11327, bs.Kredit);
            Assert.Equal(559.43m, bs.End);
            
            Assert.Equal(4, bs.Items.Count);
            
            int itemNum = 0;
            Assert.Equal(new DateTime(2023, 6, 30), bs.Items[itemNum].Dt.Date);
            Assert.Equal("Актюбинский Областной Филиал Народного Банка Казахстана", bs.Items[itemNum].Receiver);
            Assert.Equal("961141000023", bs.Items[itemNum].ReceiverBin);
            Assert.Equal("Потребительский кооператив \"Авиатор-4\"", bs.Items[itemNum].Sender);
            Assert.Equal("980340019608", bs.Items[itemNum].SenderBin);
            Assert.Equal(1000, bs.Items[itemNum].Amount);
            Assert.Equal(OperationTypeCodes.DEBET, bs.Items[itemNum].OperationTypeCode);

            itemNum = 3;
            Assert.Equal(new DateTime(2023, 6, 9), bs.Items[itemNum].Dt.Date);
            Assert.Equal("Потребительский Кооператив \"Авиатор-4\"", bs.Items[itemNum].Receiver);
            Assert.Equal("980340019608", bs.Items[itemNum].ReceiverBin);
            Assert.Equal("Бижанова Алия Сарантаевна", bs.Items[itemNum].Sender);
            Assert.Equal("750302402901", bs.Items[itemNum].SenderBin);
            Assert.Equal(11500, bs.Items[itemNum].Amount);
            Assert.Equal(OperationTypeCodes.KREDIT, bs.Items[itemNum].OperationTypeCode);
        }
    }
}
