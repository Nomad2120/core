using Microsoft.AspNetCore.Mvc.RazorPages;
using OSI.Core.Logic;
using OSI.Core.Models.Db;
using OSI.Core.Models.Reports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OSI.Core.Tests
{
    public class TestPrintInvoice
    {
        private readonly string connectionString = "Host=10.1.1.125;Database=osi_billing;Username=postgres;Password=Aa222111";
        private readonly int testOsiId = 39; // ОСИ Шумахер

        public TestPrintInvoice()
        {
            OSIBillingDbContext.ConnectionString = connectionString;
        }

        [Fact]
        public void TestPrint()
        {
            string osiName = "ОСИ \"Кондоминиум дома 133Г по ул.Бокенбай батыра\"";
            string address = "г.Актобе, ул.Ленина 1";
            string period = "апрель 2022";
            List<OSVAbonent> osvAbonents = new List<OSVAbonent>
            {
                new OSVAbonent
                {
                    AbonentId = 1234,
                    AbonentName = "Иванов А.А",
                    Flat = "44",
                    ServicesSaldo = new Dictionary<string, OSVSaldo>
                    {
                        {"Услуги управляющей компании", new OSVSaldo
                            {
                                Begin = 500,
                                Debet = 350,
                                Kredit = 0,
                                End = 850
                            }
                        },
                        {"Техническое обслуживание", new OSVSaldo
                            {
                                Begin = 100,
                                Debet = 300,
                                Kredit = 200,
                                End = 200
                            }
                        },
                    }
                },
                new OSVAbonent
                {
                    AbonentId = 2233,
                    AbonentName = "Сидоров А.А",
                    Flat = "45",
                    ServicesSaldo = new Dictionary<string, OSVSaldo>
                    {
                        {"Коммунальные услуги в местах общего пользования", new OSVSaldo
                            {
                                Begin = 1000,
                                Debet = 500,
                                Kredit = 700,
                                End = 800
                            }
                        },
                        {"Уборка помещений (услуги клининга)", new OSVSaldo
                            {
                                Begin = 3000,
                                Debet = 500,
                                Kredit = 1000,
                                End = 2500
                            }
                        },
                    }
                },
            };
            string result = ""; // PrintInvoiceLogic.GetInvoicesByListOfAbonents(osvAbonents, osiName, address, period);
            File.WriteAllText(@"d:\aa\2.html", result);
            Assert.True(true);
        }

        [Fact]
        public void TestNaturalComparerOrder()
        {
            List<OSVAbonent> osvAbonents = new List<OSVAbonent>
            {
                new OSVAbonent { Flat = "5" },
                new OSVAbonent { Flat = "4" },
                new OSVAbonent { Flat = "7а" },
                new OSVAbonent { Flat = "7" },
                new OSVAbonent { Flat = "1" },
                new OSVAbonent { Flat = "6" },
                new OSVAbonent { Flat = "2" },
                new OSVAbonent { Flat = "3" },
                new OSVAbonent { Flat = "НП-3" },
                new OSVAbonent { Flat = "8" },
            };
            List<OSVAbonent> resultList = PrintInvoiceLogic.GetNaturalComparerOrderList(osvAbonents);
            Assert.Equal("1", resultList[0].Flat);
            Assert.Equal("2", resultList[1].Flat);
            Assert.Equal("3", resultList[2].Flat);
            Assert.Equal("4", resultList[3].Flat);
            Assert.Equal("5", resultList[4].Flat);
            Assert.Equal("6", resultList[5].Flat);
            Assert.Equal("7", resultList[6].Flat);
            Assert.Equal("7а", resultList[7].Flat);
            Assert.Equal("8", resultList[8].Flat);
            Assert.Equal("НП-3", resultList[9].Flat);
        }

        [Fact]
        public void TestStraightPrintOrder()
        {
            List<OSVAbonent> osvAbonents = new List<OSVAbonent>
            {
                new OSVAbonent { Flat = "5" },
                new OSVAbonent { Flat = "4" },
                new OSVAbonent { Flat = "7а" },
                new OSVAbonent { Flat = "7" },
                new OSVAbonent { Flat = "1" },
                new OSVAbonent { Flat = "6" },
                new OSVAbonent { Flat = "2" },
                new OSVAbonent { Flat = "3" },
                new OSVAbonent { Flat = "НП-3" },
                new OSVAbonent { Flat = "8" },
            };
            Dictionary<OSVAbonent, bool> resultList = PrintInvoiceLogic.GetStraightPrintOrder(osvAbonents, 3);

            Assert.Equal("1", resultList.Keys.ElementAt(0).Flat);
            Assert.False(resultList.Values.ElementAt(0));
            Assert.Equal("2", resultList.Keys.ElementAt(1).Flat);
            Assert.False(resultList.Values.ElementAt(1));
            Assert.Equal("3", resultList.Keys.ElementAt(2).Flat);
            Assert.True(resultList.Values.ElementAt(2));

            Assert.Equal("4", resultList.Keys.ElementAt(3).Flat);
            Assert.False(resultList.Values.ElementAt(3));
            Assert.Equal("5", resultList.Keys.ElementAt(4).Flat);
            Assert.False(resultList.Values.ElementAt(4));
            Assert.Equal("6", resultList.Keys.ElementAt(5).Flat);
            Assert.True(resultList.Values.ElementAt(5));

            Assert.Equal("7", resultList.Keys.ElementAt(6).Flat);
            Assert.False(resultList.Values.ElementAt(6));
            Assert.Equal("7а", resultList.Keys.ElementAt(7).Flat);
            Assert.False(resultList.Values.ElementAt(7));
            Assert.Equal("8", resultList.Keys.ElementAt(8).Flat);
            Assert.True(resultList.Values.ElementAt(8));

            Assert.Equal("НП-3", resultList.Keys.ElementAt(9).Flat);
            Assert.True(resultList.Values.ElementAt(9));
        }

        [Fact]
        public void TestThroughPrintOrder()
        {
            List<OSVAbonent> osvAbonents = new List<OSVAbonent>
            {
                new OSVAbonent { Flat = "5" },
                new OSVAbonent { Flat = "4" },
                new OSVAbonent { Flat = "7а" },
                new OSVAbonent { Flat = "7" },
                new OSVAbonent { Flat = "1" },
                new OSVAbonent { Flat = "6" },
                new OSVAbonent { Flat = "2" },
                new OSVAbonent { Flat = "3" },
                new OSVAbonent { Flat = "НП-3" },
                new OSVAbonent { Flat = "8" },
            };
            Dictionary<OSVAbonent, bool> resultList = PrintInvoiceLogic.GetThroughPrintOrder(osvAbonents, 3);
// при кол-ве элементов в 10 шт и 3 инвойсах на страницу получаем такой порядок
//length = 10
//incStep = 4
//new list
//1
//5
//9
//last on page
//new list
//2
//6
//10
//last on page
//new list
//3
//7
//last on page
//new list
//4
//8
//last on page

            Assert.Equal("1", resultList.Keys.ElementAt(0).Flat);
            Assert.False(resultList.Values.ElementAt(0));            
            Assert.Equal("5", resultList.Keys.ElementAt(1).Flat);
            Assert.False(resultList.Values.ElementAt(1));            
            Assert.Equal("8", resultList.Keys.ElementAt(2).Flat);
            Assert.True(resultList.Values.ElementAt(2));
            
            Assert.Equal("2", resultList.Keys.ElementAt(3).Flat);
            Assert.False(resultList.Values.ElementAt(3));
            Assert.Equal("6", resultList.Keys.ElementAt(4).Flat);
            Assert.False(resultList.Values.ElementAt(4));
            Assert.Equal("НП-3", resultList.Keys.ElementAt(5).Flat);
            Assert.True(resultList.Values.ElementAt(5));

            Assert.Equal("3", resultList.Keys.ElementAt(6).Flat);
            Assert.False(resultList.Values.ElementAt(6));
            Assert.Equal("7", resultList.Keys.ElementAt(7).Flat);
            Assert.True(resultList.Values.ElementAt(7));

            Assert.Equal("4", resultList.Keys.ElementAt(8).Flat);
            Assert.False(resultList.Values.ElementAt(8));
            Assert.Equal("7а", resultList.Keys.ElementAt(9).Flat);
            Assert.True(resultList.Values.ElementAt(9));
        }

        [Fact]
        public void TestNoResidentsList()
        {
            List<OSVAbonent> osvAbonents = new List<OSVAbonent>
            {
                new OSVAbonent { Flat = "5", AreaTypeCode = Models.Enums.AreaTypeCodes.RESIDENTIAL },
                new OSVAbonent { Flat = "4", AreaTypeCode = Models.Enums.AreaTypeCodes.NON_RESIDENTIAL },
                new OSVAbonent { Flat = "7а", AreaTypeCode = Models.Enums.AreaTypeCodes.BASEMENT },
                new OSVAbonent { Flat = "7", AreaTypeCode = Models.Enums.AreaTypeCodes.RESIDENTIAL },
            };
            List<OSVAbonent> resultList = PrintInvoiceLogic.GetOnlyResidents(osvAbonents);
            Assert.DoesNotContain(resultList, o => o.AreaTypeCode == Models.Enums.AreaTypeCodes.NON_RESIDENTIAL);
            Assert.Contains(resultList, o => o.AreaTypeCode == Models.Enums.AreaTypeCodes.RESIDENTIAL);
            Assert.Contains(resultList, o => o.AreaTypeCode == Models.Enums.AreaTypeCodes.BASEMENT);
        }
    }
}
