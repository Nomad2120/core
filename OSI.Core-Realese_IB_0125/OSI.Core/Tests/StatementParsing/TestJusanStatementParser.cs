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
    public class TestJusanStatementParser
    {
        public TestJusanStatementParser()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // дает доступ к кодировкам cp866 и win1251
        }

        [Fact]
        public void TestJusanFormat()
        {
            IStatementParser jusanParsing = new JusanBankStatementParser();
            byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\JusanFiles\jusan.txt");
            bool rightFormat = jusanParsing.CheckFileFormat(data);
            Assert.True(rightFormat);
        }

        [Fact]
        public void TestJusanGetAccount()
        {
            var jusanParsing = new JusanBankStatementParser();
            string text = @"1CClientBankExchange
Кодировка=windows-1251
ВерсияФормата=2.00
ДатаНачала=01.01.2024
ДатаКонца=31.01.2024
РасчСчет=KZ72998LTB0001497589";
            var lines = text.Replace("\r", "").Split('\n').ToList();
            var account = jusanParsing.GetAccount(lines);
            Assert.Equal("KZ72998LTB0001497589", account);

            lines = new List<string>();
            Assert.ThrowsAny<Exception>(() => { _ = jusanParsing.GetAccount(lines); });

            lines = new List<string>
            {
                "", "", "", "", "", "ыва=йцу"
            };
            Assert.ThrowsAny<Exception>(() => { _ = jusanParsing.GetAccount(lines); });
        }

        [Fact]
        public void TestJusanGetPeriodBegin()
        {
            var jusanParsing = new JusanBankStatementParser();
            string text = @"1CClientBankExchange
Кодировка=windows-1251
ВерсияФормата=2.00
ДатаНачала=01.01.2024
ДатаКонца=31.01.2024
РасчСчет=KZ72998LTB0001497589";
            var lines = text.Replace("\r", "").Split('\n').ToList();
            var dt = jusanParsing.GetPeriodBegin(lines);
            Assert.Equal(new DateTime(2024, 1, 1), dt);

            // пустой файл
            lines = new List<string>();
            Assert.ThrowsAny<Exception>(() => { _ = jusanParsing.GetPeriodBegin(lines); });

            // дата1 дата2 вместо датаначала датаконца
            text = @"1CClientBankExchange
Кодировка=windows-1251
ВерсияФормата=2.00
Дата1=01.01.2024
Дата2=31.01.2024
РасчСчет=KZ72998LTB0001497589";
            lines = text.Replace("\r", "").Split('\n').ToList();
            Assert.ThrowsAny<Exception>(() => { _ = jusanParsing.GetPeriodBegin(lines); });

            // неверно прописана дата
            text = @"1CClientBankExchange
Кодировка=windows-1251
ВерсияФормата=2.00
ДатаНачала=фыв
ДатаКонца=йцу
РасчСчет=KZ72998LTB0001497589";
            lines = text.Replace("\r", "").Split('\n').ToList();
            Assert.ThrowsAny<Exception>(() => { _ = jusanParsing.GetPeriodBegin(lines); });
        }

        [Fact]
        public void TestJusanGetPeriodEnd()
        {
            string text = @"1CClientBankExchange
Кодировка=windows-1251
ВерсияФормата=2.00
ДатаНачала=01.01.2024
ДатаКонца=31.01.2024
РасчСчет=KZ72998LTB0001497589";
            var lines = text.Replace("\r", "").Split('\n').ToList();

            var jusanParsing = new JusanBankStatementParser();
            var dt = jusanParsing.GetPeriodEnd(lines);
            Assert.Equal(new DateTime(2024, 1, 31), dt);

            lines[4] = "КОНЕЦ=31.01.2024";
            Assert.ThrowsAny<Exception>(() => { var dt = jusanParsing.GetPeriodEnd(lines); });

            lines[4] = "ДатаКонца=asd";
            Assert.ThrowsAny<Exception>(() => { var dt = jusanParsing.GetPeriodEnd(lines); });

            lines = new List<string>();
            Assert.ThrowsAny<Exception>(() => { var dt = jusanParsing.GetPeriodEnd(lines); });
        }

        [Fact]
        public void TestJusanCalcTotalAmounts()
        {
            string text = @"1CClientBankExchange
Кодировка=windows-1251
ВерсияФормата=2.00
ДатаНачала=01.01.2024
ДатаКонца=31.01.2024
РасчСчет=KZ72998LTB0001497589
СЕКЦИЯРАСЧСЧЕТ
ДатаНачала=01.01.2024
ДатаКонца=31.01.2024
РасчСчет=KZ72998LTB0001497589
НачальныйОстаток=1020312.05
ВсегоПоступило=406219.14
ВсегоСписано=110860
КонечныйОстаток=1315671.19
КОНЕЦРАСЧСЧЕТ
СЕКЦИЯДОКУМЕНТ=Выписка
НомерДокумента=95022
ДатаДокумента=03.01.2024
ВидДокумента=ПлатежноеПоручение
СуммаПриход=7745.7
ПолучательНаименование=""Объединение собственников имущества многоквартирного жилого дома улица Лихарева, 7""
ПолучательБИН_ИИН=220640014948
ПолучательИИК=KZ72998LTB0001497589
ПлательщикНаименование=АО ""KASPI BANK""
ПлательщикБИН_ИИН=971240001315
ПлательщикИИК=KZ81722S000001159377
ДатаОперации=03.01.2024
НазначениеПлатежа=Принятые платежи за 29.12.2023 ком. 74.30 тг. (7820.00 - 0.00 ) Общая сумма платежей 7820.00
ПлательщикБанкБИК=TSESKZKA
КонецДокумента
";
            var lines = text.Replace("\r", "").Split('\n').ToList();
            var jusanParsing = new JusanBankStatementParser();
            var totals = jusanParsing.GetTotalAmounts(lines);
            Assert.Equal(1020312.05m, totals.Begin);
            Assert.Equal(406219.14m, totals.Debet);
            Assert.Equal(110860m, totals.Kredit);
            Assert.Equal(1315671.19m, totals.End);
            lines = new List<string>();
            totals = jusanParsing.GetTotalAmounts(lines);
            Assert.Equal(0, totals.Begin);
            Assert.Equal(0, totals.Debet);
            Assert.Equal(0, totals.Kredit);
            Assert.Equal(0, totals.End);
        }

        [Fact]
        public void TestJusanParseAllData()
        {
            var jusanParsing = new JusanBankStatementParser();
            byte[] data = File.ReadAllBytes(@"Tests\StatementParsing\JusanFiles\jusan.txt");
            var bs = jusanParsing.ParseData(data);
            Assert.Equal("KZ72998LTB0001497589", bs.Account);
            Assert.Equal(new DateTime(2024, 1, 1), bs.PeriodStart);
            Assert.Equal(new DateTime(2024, 1, 31), bs.PeriodEnd);
            Assert.Equal(1020312.05m, bs.Begin);
            Assert.Equal(406219.14m, bs.Debet);
            Assert.Equal(110860m, bs.Kredit);
            Assert.Equal(1315671.19m, bs.End);

            Assert.Equal(39, bs.Items.Count);

            int itemNum = 0;
            Assert.Equal(new DateTime(2024, 1, 3), bs.Items[itemNum].Dt.Date);
            Assert.Equal("\"Объединение собственников имущества многоквартирного жилого дома улица Лихарева, 7\"", bs.Items[itemNum].Receiver);
            Assert.Equal("220640014948", bs.Items[itemNum].ReceiverBin); 
            Assert.Equal("АО \"KASPI BANK\"", bs.Items[itemNum].Sender);
            Assert.Equal("971240001315", bs.Items[itemNum].SenderBin);
            Assert.Equal(7745.7m, bs.Items[itemNum].Amount);
            Assert.Equal(OperationTypeCodes.KREDIT, bs.Items[itemNum].OperationTypeCode);

            itemNum = 4;
            Assert.Equal(new DateTime(2024, 1, 4), bs.Items[itemNum].Dt.Date);
            Assert.Equal("АО \"Jusan Bank\"", bs.Items[itemNum].Receiver);
            Assert.Equal("050141000631", bs.Items[itemNum].ReceiverBin);
            Assert.Equal("\"Объединение собственников имущества многоквартирного жилого дома улица Лихарева, 7\"", bs.Items[itemNum].Sender);
            Assert.Equal("220640014948", bs.Items[itemNum].SenderBin);
            Assert.Equal(1000m, bs.Items[itemNum].Amount);
            Assert.Equal(OperationTypeCodes.DEBET, bs.Items[itemNum].OperationTypeCode);
        }
    }
}
