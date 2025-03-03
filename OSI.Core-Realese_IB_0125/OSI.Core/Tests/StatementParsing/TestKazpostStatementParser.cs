using Blazorise.Extensions;
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
    public class TestKazpostStatementParser
    {
        private List<string> allLines;

        public TestKazpostStatementParser()
        {
            allLines = File.ReadAllLines(@"Tests\StatementParsing\KazpostFiles\kazpost.txt").Select(s => s.TrimStart(' ')).ToList();
        }

        [Fact]
        public void TestKazpostFormat()
        {
            IStatementParser kazpostParsing = new KazpostStatementParser();
            byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\KazpostFiles\kazpost.txt");
            bool rightFormat = kazpostParsing.CheckFileFormat(data);
            Assert.True(rightFormat);
        }

        [Fact]
        public void TestKazpostCalcTotalAmounts()
        {            
            var kazpostParsing = new KazpostStatementParser();
            var totals = kazpostParsing.GetTotalAmounts(allLines);
            Assert.Equal(455336.52m, totals.Begin);
            Assert.Equal(1022238.68m, totals.Debet);
            Assert.Equal(856308, totals.Kredit);
            Assert.Equal(621267.2m, totals.End);
            var lines = new List<string>();
            totals = kazpostParsing.GetTotalAmounts(lines);
            Assert.Equal(0, totals.Begin);
            Assert.Equal(0, totals.Debet);
            Assert.Equal(0, totals.Kredit);
            Assert.Equal(0, totals.End);
        }

        [Fact]
        public void TestKazpostGetPeriodBegin()
        {
            var kazpostParsing = new KazpostStatementParser();
            var dt = kazpostParsing.GetPeriodBegin(allLines);
            Assert.Equal(new DateTime(2024, 4, 1), dt);
                        
            var lines = new List<string>();
            Assert.ThrowsAny<Exception>(() =>
            {
                var dt = kazpostParsing.GetPeriodBegin(lines);
            });
            
            lines = new List<string>
            {
                "1CClientBankExchange",
                "НАЧАЛО=01.06.2023",
                "КОНЕЦ=30.06.2023",
            };
            Assert.ThrowsAny<Exception>(() =>
            {
                var dt = kazpostParsing.GetPeriodBegin(lines);
            });

            lines = new List<string>
            {
                "1CClientBankExchange",
                "ДАТАНАЧАЛА=ываыва"
            }; 
            Assert.ThrowsAny<Exception>(() =>
            {
                var dt = kazpostParsing.GetPeriodBegin(lines);
            });
        }

        [Fact]
        public void TestKazpostGetPeriodEnd()
        {
            var kazpostParsing = new KazpostStatementParser();
            var dt = kazpostParsing.GetPeriodEnd(allLines);
            Assert.Equal(new DateTime(2024, 4, 30), dt);

            var lines = new List<string>();
            Assert.ThrowsAny<Exception>(() => { var dt = kazpostParsing.GetPeriodEnd(lines); });

            lines = new List<string>
            {
                "1CClientBankExchange",
                "НАЧАЛО=01.06.2023",
                "КОНЕЦ=30.06.2023",
            };
            Assert.ThrowsAny<Exception>(() => { var dt = kazpostParsing.GetPeriodEnd(lines);});

            lines = new List<string>
            {
                "1CClientBankExchange",
                "ДАТАНАЧАЛА=ываыва",
                "ДАТАКОНЦА=ываыва",
            };
            Assert.ThrowsAny<Exception>(() => { var dt = kazpostParsing.GetPeriodEnd(lines); });
        }

        [Fact]
        public void TestKazpostGetAccount()
        {            
            var kazpostParsing = new KazpostStatementParser();
            var account = kazpostParsing.GetAccount(allLines);
            Assert.Equal("KZ19563D350000091934", account);

            var lines = new List<string>();
            Assert.ThrowsAny<Exception>(() => { var dt = kazpostParsing.GetAccount(lines); });

            lines = new List<string>
            {
                "", "", "", "ыва=йцу"
            };
            Assert.ThrowsAny<Exception>(() => { var dt = kazpostParsing.GetAccount(lines); });
        }

        [Fact]
        public void TestKazpostParseAllData()
        {
            byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\KazpostFiles\kazpost.txt");
            var kazpostParsing = new KazpostStatementParser();
            var bs = kazpostParsing.ParseData(data);
            Assert.Equal("KZ19563D350000091934", bs.Account);
            Assert.Equal(new DateTime(2024, 4, 1), bs.PeriodStart);
            Assert.Equal(new DateTime(2024, 4, 30), bs.PeriodEnd);
            Assert.Equal(455336.52m, bs.Begin);
            Assert.Equal(1022238.68m, bs.Debet);
            Assert.Equal(856308, bs.Kredit);
            Assert.Equal(621267.2m, bs.End);

            Assert.Equal(77, bs.Items.Count);
            
            int itemNum = 0;
            Assert.Equal(new DateTime(2024, 4, 2), bs.Items[itemNum].Dt.Date);
            Assert.Equal("Кооператив собственников квартир \"Дом №67 проспект Абулхаир хана\"", bs.Items[itemNum].Receiver);
            Assert.Equal("190540007001", bs.Items[itemNum].ReceiverBin);
            Assert.Equal("АО \"KASPI BANK\"", bs.Items[itemNum].Sender);
            Assert.Equal("971240001315", bs.Items[itemNum].SenderBin);
            Assert.Equal("Согласно ведомости распределения '2359591' от 01/04/2024Оплата ком. услуг", bs.Items[itemNum].Assign);
            Assert.Equal(5490.39m, bs.Items[itemNum].Amount);
            Assert.Equal(OperationTypeCodes.KREDIT, bs.Items[itemNum].OperationTypeCode);

            itemNum = 2;
            Assert.Equal(new DateTime(2024, 4, 3), bs.Items[itemNum].Dt.Date);
            Assert.Equal("Актюбинский  ОФ", bs.Items[itemNum].Receiver);
            Assert.Equal("991141005125", bs.Items[itemNum].ReceiverBin);
            Assert.Equal("Кооператив собственников квартир \"Дом №67 проспект Абулхаир хана\"", bs.Items[itemNum].Sender);
            Assert.Equal("190540007001", bs.Items[itemNum].SenderBin);
            Assert.Equal("Комиссия за операцию \"Перевод зарплаты на карточный счет\" согласно тарифам банкаРасчетный документ № 1 от 03.04.24 на сумму 100 000.00 KZT, НДС не облагается.", bs.Items[itemNum].Assign);
            Assert.Equal(600, bs.Items[itemNum].Amount);
            Assert.Equal(OperationTypeCodes.DEBET, bs.Items[itemNum].OperationTypeCode);
        }
    }
}
