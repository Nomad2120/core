using OSI.Core.Logic.BankStatementParsing;
using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace OSI.Core.Tests.StatementParsing
{
    public class TestBccTxtStatementParser
    {
        public TestBccTxtStatementParser()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // дает доступ к кодировкам cp866 и win1251
        }

        [Fact]
        public void TestBccFormat()
        {
            IStatementParser bccParsing = new BccBankTxtStatementParser();
            byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\BccFiles\KZ698562203124676325 с 1.10.24 по 31.10.24.txt");
            bool rightFormat = bccParsing.CheckFileFormat(data);
            Assert.True(rightFormat);
            data = File.ReadAllBytes(@"Tests\StatementParsing\KaspiFiles\kaspi.xlsx");
            rightFormat = bccParsing.CheckFileFormat(data);
            Assert.False(rightFormat);
        }

        [Fact]
        public void TestBccCalcTotalAmounts()
        {
            string text = @"1CClientBankExchange
ВерсияФормата=2.0
Отправитель=АО ""Банк ЦентрКредит""
Получатель=1C Предприятие, Бухгалтерия для Казахстана
ДатаСоздания=01.11.2024
ВремяСоздания=18:11:22
ДатаНачала=01.10.2024
ДатаКонца=31.10.2024
РасчСчет=KZ698562203124676325
СекцияРасчСчет
ДатаНачала=01.10.2024
ДатаКонца=31.10.2024
РасчСчет=KZ698562203124676325
НачальныйОстаток=202742.8
ВсегоПоступило=236768.22
ВсегоСписано=419496.99
КонечныйОстаток=20014.03
КонецРасчСчет
СекцияДокумент=""Выписка""
ВидДокумента=ПлатежноеПоручение
ДатаОперации=01.10.2024
СуммаРасход=160000
НомерДокумента=NT-475719
ДатаДокумента=01.10.2024
ПлательщикНаименование=Объединение собственников имущества ""Дружба-13""
ПлательщикБИН_ИИН=220840049859
ПлательщикКБЕ=18
ПлательщикИИК=KZ698562203124676325
ПлательщикБанкНаименование=Акционерное Общество ""Банк ЦентрКредит""
ПлательщикБанкБИК=KCJBKZKX
ПолучательНаименование=Филиал Акционерного Общества ""Банк ЦентрКредит"" в городе Актобе
ПолучательБИН_ИИН=901041000015
ПолучательКБЕ=18
ПолучательИИК=KZ298561005127073713
ПолучательБанкНаименование=АО ""Банк ЦентрКредит""
ПолучательБанкБИК=KCJBKZKX
НазначениеПлатежа=Снятие наличных АТМ 00051289, 01.10.2024, ATB Abiyl.khana 19 OTD 0, KTOBEKZ, , Карта: 489988******6145
КодНазначенияПлатежа=341
ДатаВалютирования=01.10.2024
Сумма=160000
Валюта=KZT
КонецДокумента
";
            var lines = text.Replace("\r", "").Split('\n').ToList();
            var bccParsing = new BccBankTxtStatementParser();
            var totals = bccParsing.GetTotalAmounts(lines);
            Assert.Equal(202742.8m, totals.Begin);
            Assert.Equal(236768.22m, totals.Debet);
            Assert.Equal(419496.99m, totals.Kredit);
            Assert.Equal(20014.03m, totals.End);
            lines = new List<string>();
            totals = bccParsing.GetTotalAmounts(lines);
            Assert.Equal(0, totals.Begin);
            Assert.Equal(0, totals.Debet);
            Assert.Equal(0, totals.Kredit);
            Assert.Equal(0, totals.End);
        }

        [Fact]
        public void TestBccGetPeriodBegin()
        {
            string text = @"1CClientBankExchange
ВерсияФормата=2.0
Отправитель=АО ""Банк ЦентрКредит""
Получатель=1C Предприятие, Бухгалтерия для Казахстана
ДатаСоздания=01.11.2024
ВремяСоздания=18:11:22
ДатаНачала=01.10.2024
ДатаКонца=31.10.2024
РасчСчет=KZ698562203124676325";
            var lines = text.Replace("\r", "").Split('\n').ToList();

            var bccParsing = new BccBankTxtStatementParser();
            var dt = bccParsing.GetPeriodBegin(lines);
            Assert.Equal(new DateTime(2024, 10, 1), dt);
                        
            lines = new List<string>();
            Assert.ThrowsAny<Exception>(() =>
            {
                var dt = bccParsing.GetPeriodBegin(lines);
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
                var dt = bccParsing.GetPeriodBegin(lines);
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
                var dt = bccParsing.GetPeriodBegin(lines);
            });
        }

        [Fact]
        public void TestBccGetPeriodEnd()
        {
            string text = @"1CClientBankExchange
ВерсияФормата=2.0
Отправитель=АО ""Банк ЦентрКредит""
Получатель=1C Предприятие, Бухгалтерия для Казахстана
ДатаСоздания=01.11.2024
ВремяСоздания=18:11:22
ДатаНачала=01.10.2024
ДатаКонца=31.10.2024
РасчСчет=KZ698562203124676325";
            var lines = text.Replace("\r", "").Split('\n').ToList();

            var bccParsing = new BccBankTxtStatementParser();
            var dt = bccParsing.GetPeriodEnd(lines);
            Assert.Equal(new DateTime(2024, 10, 31), dt);

            lines[7] = "КОНЕЦ=30.06.2023";
            Assert.ThrowsAny<Exception>(() => { var dt = bccParsing.GetPeriodEnd(lines);});

            lines[7] = "ДатаКонца=asd";
            Assert.ThrowsAny<Exception>(() => { var dt = bccParsing.GetPeriodEnd(lines); });

            lines = new List<string>();
            Assert.ThrowsAny<Exception>(() => { var dt = bccParsing.GetPeriodEnd(lines); });
        }

        [Fact]
        public void TestBccGetAccount()
        {
            string text = @"1CClientBankExchange
ВерсияФормата=2.0
Отправитель=АО ""Банк ЦентрКредит""
Получатель=1C Предприятие, Бухгалтерия для Казахстана
ДатаСоздания=01.11.2024
ВремяСоздания=18:11:22
ДатаНачала=01.10.2024
ДатаКонца=31.10.2024
РасчСчет=KZ698562203124676325";
            var lines = text.Replace("\r", "").Split('\n').ToList();
            var bccParsing = new BccBankTxtStatementParser();
            var account = bccParsing.GetAccount(lines);
            Assert.Equal("KZ698562203124676325", account);

            lines = new List<string>();
            Assert.ThrowsAny<Exception>(() => { var dt = bccParsing.GetAccount(lines); });

            lines = new List<string>
            {
                "", "", "", "", "", "", "", "", "ыва=йцу"
            };
            Assert.ThrowsAny<Exception>(() => { var dt = bccParsing.GetAccount(lines); });
        }

        [Fact]
        public void TestBccParseAllData()
        {
            byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\BccFiles\KZ698562203124676325 с 1.10.24 по 31.10.24.txt");
            var bccParsing = new BccBankTxtStatementParser();
            var bs = bccParsing.ParseData(data);
            Assert.Equal("KZ698562203124676325", bs.Account);
            Assert.Equal(new DateTime(2024, 10, 1), bs.PeriodStart);
            Assert.Equal(new DateTime(2024, 10, 31), bs.PeriodEnd);
            Assert.Equal(202742.8m, bs.Begin);
            Assert.Equal(236768.22m, bs.Debet);
            Assert.Equal(419496.99m, bs.Kredit);
            Assert.Equal(20014.03m, bs.End);

            Assert.Equal(46, bs.Items.Count);

            int itemNum = 0;
            var item = bs.Items[itemNum];
            Assert.Equal(new DateTime(2024, 10, 1), bs.Items[itemNum].Dt.Date);
            Assert.Equal("Филиал Акционерного Общества \"Банк ЦентрКредит\" в городе Актобе", bs.Items[itemNum].Receiver);
            Assert.Equal("901041000015", bs.Items[itemNum].ReceiverBin);
            Assert.Equal("Объединение собственников имущества \"Дружба-13\"", bs.Items[itemNum].Sender);
            Assert.Equal("220840049859", bs.Items[itemNum].SenderBin);
            Assert.Equal(160000, bs.Items[itemNum].Amount);
            Assert.Equal(OperationTypeCodes.DEBET, bs.Items[itemNum].OperationTypeCode);

            itemNum = 2;
            item = bs.Items[itemNum];
            Assert.Equal(new DateTime(2024, 10, 4), bs.Items[itemNum].Dt.Date);
            Assert.Equal("Объединение собственников имущества \"Дружба-13\"", bs.Items[itemNum].Receiver);
            Assert.Equal("220840049859", bs.Items[itemNum].ReceiverBin);
            Assert.Equal("АО \"KASPI BANK\"", bs.Items[itemNum].Sender);
            Assert.Equal("971240001315", bs.Items[itemNum].SenderBin);
            Assert.Equal(1597.19m, bs.Items[itemNum].Amount);
            Assert.Equal(OperationTypeCodes.KREDIT, bs.Items[itemNum].OperationTypeCode);
        }
    }
}
