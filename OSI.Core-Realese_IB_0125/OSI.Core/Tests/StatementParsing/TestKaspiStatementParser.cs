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
    public class TestKaspiStatementParser
    {
        [Fact]
        public void TestKaspiFormat()
        {
            IStatementParser kaspiParsing = new KaspiBankStatementParser();
            byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\KaspiFiles\kaspi.txt");
            bool rightFormat = kaspiParsing.CheckFileFormat(data);
            Assert.True(rightFormat);
            data = File.ReadAllBytes(@"Tests\StatementParsing\KaspiFiles\kaspi.xlsx");
            rightFormat = kaspiParsing.CheckFileFormat(data);
            Assert.False(rightFormat);
        }

        [Fact]
        public void TestKaspiCalcTotalAmounts()
        {
            string text = @"1CClientBankExchange
ВерсияФормата=1.01
Кодировка=Windows
Получатель=1C
ДатаНачала=01.06.2023
ДатаКонца=30.06.2023
РасчСчет=KZ79722S000001674774
СекцияРасчСчет
ДатаНачала=01.06.2023
ДатаКонца=30.06.2023
РасчСчет=KZ79722S000001674774
НачальныйОстаток=129464.83
ВсегоПоступило=2188673.42
ВсегоСписано=2007812.12
КонечныйОстаток=310326.13
КонецРасчСчет
СекцияДокумент=выписка
НомерДокумента=47437099
ДатаДокумента=30.06.2023
ВидДокумента=Платежное поручение
ПолучательНаименование=Яна Петровна О.
ПолучательБИН_ИИН=910308400020
ПолучательИИК=KZ22722C000024901213
ПолучательБанкБИК=
ПолучательКБЕ=1
ПлательщикНаименование=ОСИ  ""ЖК ""Юнис Сити""
ПлательщикБИН_ИИН=181140004063
ПлательщикИИК=KZ79722S000001674774
ПлательщикБанкБИК=CASPKZKA
ПлательщикКБЕ=18
СуммаРасход=4000.00
ДатаОперации=30.06.2023
НазначениеПлатежа=Прочие переводы денег на карту Kaspi Gold *3305
КодНазначенияПлатежа=119
КонецДокумента
";
            var lines = text.Replace("\r", "").Split('\n').ToList();
            var kaspiParsing = new KaspiBankStatementParser();
            var totals = kaspiParsing.GetTotalAmounts(lines);
            Assert.Equal(129464.83m, totals.Begin);
            Assert.Equal(2188673.42m, totals.Debet);
            Assert.Equal(2007812.12m, totals.Kredit);
            Assert.Equal(310326.13m, totals.End);
            lines = new List<string>();
            totals = kaspiParsing.GetTotalAmounts(lines);
            Assert.Equal(0, totals.Begin);
            Assert.Equal(0, totals.Debet);
            Assert.Equal(0, totals.Kredit);
            Assert.Equal(0, totals.End);
        }

        [Fact]
        public void TestKaspiGetPeriodBegin()
        {
            string text = @"1CClientBankExchange
ВерсияФормата=1.01
Кодировка=Windows
Получатель=1C
ДатаНачала=01.06.2023
ДатаКонца=30.06.2023
РасчСчет=KZ79722S000001674774";
            var lines = text.Replace("\r", "").Split('\n').ToList();

            var kaspiParsing = new KaspiBankStatementParser();
            var dt = kaspiParsing.GetPeriodBegin(lines);
            Assert.Equal(new DateTime(2023, 6, 1), dt);
                        
            lines = new List<string>();
            Assert.ThrowsAny<Exception>(() =>
            {
                var dt = kaspiParsing.GetPeriodBegin(lines);
            });

            text = @"1CClientBankExchange
ВерсияФормата=1.01
Кодировка=Windows
Получатель=1C
Дата1=01.06.2023
Дата2=30.06.2023
РасчСчет=KZ79722S000001674774";
            lines = text.Replace("\r", "").Split('\n').ToList();
            Assert.ThrowsAny<Exception>(() =>
            {
                var dt = kaspiParsing.GetPeriodBegin(lines);
            });

            lines = new List<string>
            {
                "1CClientBankExchange",
                "ВерсияФормата=1.01",
                "Кодировка=Windows",
                "Получатель=1C",
                "ДатаНачала=ываыва"
            }; 
            Assert.ThrowsAny<Exception>(() =>
            {
                var dt = kaspiParsing.GetPeriodBegin(lines);
            });
        }

        [Fact]
        public void TestKaspiGetPeriodEnd()
        {
            string text = @"1CClientBankExchange
ВерсияФормата=1.01
Кодировка=Windows
Получатель=1C
ДатаНачала=01.06.2023
ДатаКонца=30.06.2023
РасчСчет=KZ79722S000001674774";
            var lines = text.Replace("\r", "").Split('\n').ToList();

            var kaspiParsing = new KaspiBankStatementParser();
            var dt = kaspiParsing.GetPeriodEnd(lines);
            Assert.Equal(new DateTime(2023, 6, 30), dt);

            lines[5] = "КОНЕЦ=30.06.2023";
            Assert.ThrowsAny<Exception>(() => { var dt = kaspiParsing.GetPeriodEnd(lines);});

            lines[5] = "ДатаКонца=asd";
            Assert.ThrowsAny<Exception>(() => { var dt = kaspiParsing.GetPeriodEnd(lines); });

            lines = new List<string>();
            Assert.ThrowsAny<Exception>(() => { var dt = kaspiParsing.GetPeriodEnd(lines); });
        }

        [Fact]
        public void TestKaspiGetAccount()
        {
            string text = @"1CClientBankExchange
ВерсияФормата=1.01
Кодировка=Windows
Получатель=1C
ДатаНачала=01.06.2023
ДатаКонца=30.06.2023
РасчСчет=KZ79722S000001674774";
            var lines = text.Replace("\r", "").Split('\n').ToList();
            var kaspiParsing = new KaspiBankStatementParser();
            var account = kaspiParsing.GetAccount(lines);
            Assert.Equal("KZ79722S000001674774", account);

            lines = new List<string>();
            Assert.ThrowsAny<Exception>(() => { var dt = kaspiParsing.GetAccount(lines); });

            lines = new List<string>
            {
                "", "", "", "", "", "", "ыва=йцу"
            };
            Assert.ThrowsAny<Exception>(() => { var dt = kaspiParsing.GetAccount(lines); });
        }

        [Fact]
        public void TestKaspiParseAllData()
        {
            byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\KaspiFiles\kaspi.txt");
            var kaspiParsing = new KaspiBankStatementParser();
            var bs = kaspiParsing.ParseData(data);
            Assert.Equal("KZ79722S000001674774", bs.Account);
            Assert.Equal(new DateTime(2023, 6, 1), bs.PeriodStart);
            Assert.Equal(new DateTime(2023, 6, 30), bs.PeriodEnd);
            Assert.Equal(129464.83m, bs.Begin);
            Assert.Equal(2188673.42m, bs.Debet);
            Assert.Equal(2007812.12m, bs.Kredit);
            Assert.Equal(310326.13m, bs.End);

            Assert.Equal(135, bs.Items.Count);

            int itemNum = 0;
            Assert.Equal(new DateTime(2023, 6, 30), bs.Items[itemNum].Dt.Date);
            Assert.Equal("Яна Петровна О.", bs.Items[itemNum].Receiver);
            Assert.Equal("910308400020", bs.Items[itemNum].ReceiverBin);
            Assert.Equal("ОСИ  \"ЖК \"Юнис Сити\"", bs.Items[itemNum].Sender);
            Assert.Equal("181140004063", bs.Items[itemNum].SenderBin);
            Assert.Equal(4000, bs.Items[itemNum].Amount);
            Assert.Equal(OperationTypeCodes.DEBET, bs.Items[itemNum].OperationTypeCode);

            itemNum = 2;
            Assert.Equal(new DateTime(2023, 6, 30), bs.Items[itemNum].Dt.Date);
            Assert.Equal("ОСИ  \"ЖК \"Юнис Сити\"", bs.Items[itemNum].Receiver);
            Assert.Equal("181140004063", bs.Items[itemNum].ReceiverBin);
            Assert.Equal("ЧСИ ТАСБОЛАТОВ ЕРЖАН ЕЛЖАНОВИЧ", bs.Items[itemNum].Sender);
            Assert.Equal("820811300397", bs.Items[itemNum].SenderBin);
            Assert.Equal(42531.32m, bs.Items[itemNum].Amount);
            Assert.Equal(OperationTypeCodes.KREDIT, bs.Items[itemNum].OperationTypeCode);            
        }
    }
}
