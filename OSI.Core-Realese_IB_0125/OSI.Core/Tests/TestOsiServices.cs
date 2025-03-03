using Microsoft.AspNetCore.Mvc;
using OSI.Core.Logic;
using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using OSI.Core.Models.Requests;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using Xunit;

namespace OSI.Core.Tests
{
    public class TestOsiServices
    {
        //private readonly OSIBillingDbContext db;

        public TestOsiServices()
        {
            //db = OSIBillingDbContext.DbContext;
        }

        [Fact]
        public void Test_AmountMRPBigRepairPercent()
        {
            var req = new OsiServiceRequest { AccuralMethodId = 1, ServiceGroupId = 2 };

            var serviceGroup = new ServiceGroup
            {
                AccountType = new AccountType { Code = AccountTypeCodes.CURRENT },
                CanChangeName = true,
                Code = "BIG_REPAIR",
                JustOne = true,
                AllowedAccuralMethods = new AllowedAccuralMethod[]
                {
                    new AllowedAccuralMethod { AccuralMethodId = 1, ServiceGroupId = 2 },
                    new AllowedAccuralMethod { AccuralMethodId = 5, ServiceGroupId = 2 }
                }
            };

            var accuralMethod = new AccuralMethod { Id = 1, Code = "TARIF_1KVM" };
                        
            Assert.Equal(req.AccuralMethodId, accuralMethod.Id); // для проверки корректности указания тестовых данных

            var osiAccounts = new List<OsiAccount>
            {
                new OsiAccount { AccountTypeCode = AccountTypeCodes.CURRENT },
                new OsiAccount { AccountTypeCode = AccountTypeCodes.SAVINGS }
            };

            var osi = new Osi { BigRepairMrpPercent = 0.005m };

            var osiServices = new List<OsiService>
            {
                new OsiService { }
            };

            decimal mrp = 3450m;
            req.Amount = 10;
            string result = OsiServiceLogic.CheckAddOrUpdateConditions(0, mrp, req, osi, null, serviceGroup, osiAccounts, accuralMethod, null);

            Assert.Equal("Сумма услуги должна быть не меньше 17.25 тг", result);

            req.Amount = 17.25m; // mrp * osi.BigRepairMrpPercent
            result = OsiServiceLogic.CheckAddOrUpdateConditions(0, mrp, req, osi, null, serviceGroup, osiAccounts, accuralMethod, null);
            Assert.Equal("", result);

            req.Amount = 17.26m;
            result = OsiServiceLogic.CheckAddOrUpdateConditions(0, mrp, req, osi, null, serviceGroup, osiAccounts, accuralMethod, null);
            Assert.Equal("", result);

            accuralMethod = new AccuralMethod { Code = "TARIF_1KVM_EFF" };
            req.Amount = 17.25m;
            result = OsiServiceLogic.CheckAddOrUpdateConditions(0, mrp, req, osi, null, serviceGroup, osiAccounts, accuralMethod, null);
            Assert.Equal("", result);

            accuralMethod = new AccuralMethod { Code = "OB_SUM_FLAT" };
            req.Amount = 11;
            result = OsiServiceLogic.CheckAddOrUpdateConditions(0, mrp, req, osi, null, serviceGroup, osiAccounts, accuralMethod, null);
            Assert.Equal("", result);

            serviceGroup.Code = "TO";
            req.Amount = 10;
            result = OsiServiceLogic.CheckAddOrUpdateConditions(0, mrp, req, osi, null, serviceGroup, osiAccounts, accuralMethod, null);
            Assert.Equal("", result);
        }

        [Fact]
        public void Test_AmountMRPBigRepairPercentByBiggestSquareRoom()
        {
            var req = new OsiServiceRequest { AccuralMethodId = 2, ServiceGroupId = 2 };

            var serviceGroup = new ServiceGroup
            {
                AccountType = new AccountType { Code = AccountTypeCodes.CURRENT },
                CanChangeName = true,
                Code = "BIG_REPAIR",
                JustOne = true,
                AllowedAccuralMethods = new AllowedAccuralMethod[]
                {
                    new AllowedAccuralMethod { AccuralMethodId = 1, ServiceGroupId = 2 },
                    new AllowedAccuralMethod { AccuralMethodId = 2, ServiceGroupId = 2 },
                    new AllowedAccuralMethod { AccuralMethodId = 5, ServiceGroupId = 2 }
                }
            };

            var accuralMethod = new AccuralMethod { Id = 2, Code = "FIX_SUM_FLAT" };

            Assert.Equal(req.AccuralMethodId, accuralMethod.Id); // для проверки корректности указания тестовых данных

            var osiAccounts = new List<OsiAccount>
            {
                new OsiAccount { AccountTypeCode = AccountTypeCodes.CURRENT },
                new OsiAccount { AccountTypeCode = AccountTypeCodes.SAVINGS }
            };

            var osi = new Osi { BigRepairMrpPercent = 0.005m };

            var osiServices = new List<OsiService>
            {
                new OsiService { }
            };

            var abonents = new Abonent[]
            {
                new Abonent { Id = 1, Square = 100 },
                new Abonent { Id = 2, Square = 90 },
                new Abonent { Id = 3, Square = 80 },
            };

            var connectedServices = new List<ConnectedService>
            {
                new ConnectedService { Abonent = abonents[0], Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { Abonent = abonents[0], Dt = new DateTime (2023, 10, 2), IsActive = false },
                new ConnectedService { Abonent = abonents[0], Dt = new DateTime (2023, 10, 3), IsActive = true },
                new ConnectedService { Abonent = abonents[1], Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { Abonent = abonents[2], Dt = new DateTime (2023, 10, 6), IsActive = true },
            };

            decimal mrp = 3450m;
            req.Amount = 10;
            string result = OsiServiceLogic.CheckAddOrUpdateConditions(0, mrp, req, osi, null, serviceGroup, osiAccounts, accuralMethod, connectedServices);
            Assert.Equal("Сумма услуги должна быть не меньше 1725.00 тг", result); // макс 100 кв.м.

            connectedServices = new List<ConnectedService>
            {
                new ConnectedService { Abonent = abonents[0], Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { Abonent = abonents[0], Dt = new DateTime (2023, 10, 2), IsActive = false },
                new ConnectedService { Abonent = abonents[1], Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { Abonent = abonents[1], Dt = new DateTime (2023, 10, 2), IsActive = false },
                new ConnectedService { Abonent = abonents[1], Dt = new DateTime (2023, 10, 3), IsActive = true },
                new ConnectedService { Abonent = abonents[2], Dt = new DateTime (2023, 10, 6), IsActive = true },
            };
            result = OsiServiceLogic.CheckAddOrUpdateConditions(0, mrp, req, osi, null, serviceGroup, osiAccounts, accuralMethod, connectedServices);
            Assert.Equal("Сумма услуги должна быть не меньше 1552.50 тг", result); // макс 90 кв.м.

            connectedServices = new List<ConnectedService>
            {
                new ConnectedService { Abonent = abonents[0], Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { Abonent = abonents[0], Dt = new DateTime (2023, 10, 2), IsActive = false },
            };
            result = OsiServiceLogic.CheckAddOrUpdateConditions(0, mrp, req, osi, null, serviceGroup, osiAccounts, accuralMethod, connectedServices);
            Assert.Equal("", result); // макс 0 кв.м.

            result = OsiServiceLogic.CheckAddOrUpdateConditions(0, mrp, req, osi, null, serviceGroup, osiAccounts, accuralMethod, null);
            Assert.Equal("", result);
        }

        [Fact]
        public void Test_GetAbonentsFromBigRepairService()
        {
            var req = new OsiServiceRequest { AccuralMethodId = 2, ServiceGroupId = 2 };

            var serviceGroup = new ServiceGroup
            {
                AccountType = new AccountType { Code = AccountTypeCodes.CURRENT },
                CanChangeName = true,
                Code = "BIG_REPAIR",
                JustOne = true,
                AllowedAccuralMethods = new AllowedAccuralMethod[]
                {
                    new AllowedAccuralMethod { AccuralMethodId = 1, ServiceGroupId = 2 },
                    new AllowedAccuralMethod { AccuralMethodId = 2, ServiceGroupId = 2 },
                    new AllowedAccuralMethod { AccuralMethodId = 5, ServiceGroupId = 2 }
                }
            };

            var accuralMethod = new AccuralMethod { Id = 2, Code = "FIX_SUM_FLAT" };

            Assert.Equal(req.AccuralMethodId, accuralMethod.Id); // для проверки корректности указания тестовых данных

            var abonents = new List<Abonent>
            {
                new Abonent { Id = 0 },
                new Abonent { Id = 1 },
                new Abonent { Id = 2 },
                new Abonent { Id = 3 },
                new Abonent { Id = 4 },
            };

            // должны выйти 0, 1, 3, 4
            var connectedServices = new List<ConnectedService>
            {
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[0], AbonentId = abonents[0].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[0], AbonentId = abonents[0].Id, Dt = new DateTime (2023, 10, 2), IsActive = false },
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[0], AbonentId = abonents[0].Id, Dt = new DateTime (2023, 10, 3), IsActive = true },
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[1], AbonentId = abonents[1].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },

                new ConnectedService { OsiServiceId = 2, Abonent = abonents[2], AbonentId = abonents[2].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[3], AbonentId = abonents[3].Id, Dt = new DateTime (2023, 10, 1), IsActive = false },
            };

            var getAbonents = OsiServiceLogic.GetOsiServiceAbonentsBigRepair(new OsiService { Id = 1 }, connectedServices, null, abonents, null);

            Assert.Equal(4, getAbonents.Count);
            Assert.Equal(0, getAbonents[0].Id);
            Assert.Equal(1, getAbonents[1].Id);
            Assert.Equal(3, getAbonents[2].Id);
            Assert.Equal(4, getAbonents[3].Id);
            Assert.True(getAbonents[0].Checked);
            Assert.True(getAbonents[1].Checked);
            Assert.False(getAbonents[2].Checked);
            Assert.False(getAbonents[3].Checked);

            // должны выйти 2, 3, 4
            getAbonents = OsiServiceLogic.GetOsiServiceAbonentsBigRepair(new OsiService { Id = 2 }, connectedServices, null, abonents, null);

            Assert.Equal(3, getAbonents.Count);
            Assert.Equal(2, getAbonents[0].Id);
            Assert.Equal(3, getAbonents[1].Id);
            Assert.Equal(4, getAbonents[2].Id);
            Assert.True(getAbonents[0].Checked);
            Assert.False(getAbonents[1].Checked);
            Assert.False(getAbonents[2].Checked);

            // никто не выйдет
            connectedServices = new List<ConnectedService>
            {
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[0], AbonentId = abonents[0].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[1], AbonentId = abonents[1].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[2], AbonentId = abonents[2].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[3], AbonentId = abonents[3].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[4], AbonentId = abonents[4].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
            };

            getAbonents = OsiServiceLogic.GetOsiServiceAbonentsBigRepair(new OsiService { Id = 2 }, connectedServices, null, abonents, null);
            Assert.Empty(getAbonents);

            // никто не выйдет
            connectedServices = new List<ConnectedService>
            {
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[0], AbonentId = abonents[0].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[1], AbonentId = abonents[1].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[2], AbonentId = abonents[2].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[3], AbonentId = abonents[3].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[4], AbonentId = abonents[4].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
            };

            getAbonents = OsiServiceLogic.GetOsiServiceAbonentsBigRepair(new OsiService { Id = 1 }, connectedServices, null, abonents, null);
            Assert.Empty(getAbonents);

            // выйдут все unchecked
            connectedServices = new List<ConnectedService>
            {
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[0], AbonentId = abonents[0].Id, Dt = new DateTime (2023, 10, 1), IsActive = false },
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[1], AbonentId = abonents[1].Id, Dt = new DateTime (2023, 10, 1), IsActive = false },
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[2], AbonentId = abonents[2].Id, Dt = new DateTime (2023, 10, 1), IsActive = false },
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[3], AbonentId = abonents[3].Id, Dt = new DateTime (2023, 10, 1), IsActive = false },
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[4], AbonentId = abonents[4].Id, Dt = new DateTime (2023, 10, 1), IsActive = false },
            };

            getAbonents = OsiServiceLogic.GetOsiServiceAbonentsBigRepair(new OsiService { Id = 1 }, connectedServices, null, abonents, null);

            Assert.Equal(5, getAbonents.Count);
            Assert.False(getAbonents[0].Checked);
            Assert.False(getAbonents[1].Checked);
            Assert.False(getAbonents[2].Checked);
            Assert.False(getAbonents[3].Checked);
            Assert.False(getAbonents[4].Checked);

            // выйдут все checked
            connectedServices = new List<ConnectedService>
            {
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[0], AbonentId = abonents[0].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[1], AbonentId = abonents[1].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[2], AbonentId = abonents[2].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[3], AbonentId = abonents[3].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 2, Abonent = abonents[4], AbonentId = abonents[4].Id, Dt = new DateTime (2023, 10, 1), IsActive = true },
            };

            getAbonents = OsiServiceLogic.GetOsiServiceAbonentsBigRepair(new OsiService { Id = 2 }, connectedServices, null, abonents, null);

            Assert.Equal(5, getAbonents.Count);
            Assert.True(getAbonents[0].Checked);
            Assert.True(getAbonents[1].Checked);
            Assert.True(getAbonents[2].Checked);
            Assert.True(getAbonents[3].Checked);
            Assert.True(getAbonents[4].Checked);

            // когда есть абонент, отключенный в одной услуге и подключенный к другой одной датой
            connectedServices = new List<ConnectedService>
            {
                new ConnectedService { OsiServiceId = 1, AbonentId = abonents[0].Id, Dt = new DateTime (2023, 10, 5), IsActive = true },

                new ConnectedService { OsiServiceId = 2, AbonentId = abonents[1].Id, Dt = new DateTime (2023, 10, 5), IsActive = false },
                new ConnectedService { OsiServiceId = 2, AbonentId = abonents[2].Id, Dt = new DateTime (2023, 10, 5), IsActive = true },

                new ConnectedService { OsiServiceId = 3, AbonentId = abonents[1].Id, Dt = new DateTime (2023, 10, 1), IsActive = false },
                new ConnectedService { OsiServiceId = 3, AbonentId = abonents[1].Id, Dt = new DateTime (2023, 10, 5), IsActive = true },
            };

            getAbonents = OsiServiceLogic.GetOsiServiceAbonentsBigRepair(new OsiService { Id = 1 }, connectedServices, null, abonents, null);
            Assert.Equal(3, getAbonents.Count);
            Assert.True(getAbonents[0].Checked);
            Assert.False(getAbonents[1].Checked);
            Assert.False(getAbonents[2].Checked);

            // еще одна вариация на предыдущую тему
            connectedServices = new List<ConnectedService>
            {
                new ConnectedService { OsiServiceId = 1, AbonentId = abonents[0].Id, Dt = new DateTime (2023, 10, 31), IsActive = false },
                new ConnectedService { OsiServiceId = 1, AbonentId = abonents[1].Id, Dt = new DateTime (2023, 10, 31), IsActive = false },
                new ConnectedService { OsiServiceId = 1, AbonentId = abonents[2].Id, Dt = new DateTime (2023, 10, 31), IsActive = true },
                new ConnectedService { OsiServiceId = 1, AbonentId = abonents[3].Id, Dt = new DateTime (2023, 10, 31), IsActive = true },
                new ConnectedService { OsiServiceId = 1, AbonentId = abonents[4].Id, Dt = new DateTime (2023, 10, 31), IsActive = true },
                new ConnectedService { OsiServiceId = 1, AbonentId = abonents[3].Id, Dt = new DateTime (2023, 11, 7), IsActive = false },

                new ConnectedService { OsiServiceId = 2, AbonentId = abonents[0].Id, Dt = new DateTime (2023, 11, 7), IsActive = true },
                new ConnectedService { OsiServiceId = 2, AbonentId = abonents[1].Id, Dt = new DateTime (2023, 11, 7), IsActive = true },

                new ConnectedService { OsiServiceId = 3, AbonentId = abonents[3].Id, Dt = new DateTime (2023, 11, 7), IsActive = true },
            };

            getAbonents = OsiServiceLogic.GetOsiServiceAbonentsBigRepair(new OsiService { Id = 1 }, connectedServices, null, abonents, null);
            Assert.Equal(2, getAbonents.Count);
            Assert.True(getAbonents[0].Checked);
            Assert.True(getAbonents[1].Checked);

            getAbonents = OsiServiceLogic.GetOsiServiceAbonentsBigRepair(new OsiService { Id = 2 }, connectedServices, null, abonents, null);
            Assert.Equal(2, getAbonents.Count);
            Assert.True(getAbonents[0].Checked);
            Assert.True(getAbonents[1].Checked);

            getAbonents = OsiServiceLogic.GetOsiServiceAbonentsBigRepair(new OsiService { Id = 3 }, connectedServices, null, abonents, null);
            Assert.Single(getAbonents);
            Assert.True(getAbonents[0].Checked);
        }

        [Fact]
        public void Test_CheckDeltaAbonentsForMinAmountForBigRepairService()
        {
            decimal mrp = 3450m;
            decimal bigRepairMrpPercent = 0.005m;

            var abonents = new List<Abonent>
            {
                new Abonent { Id = 0, Square = 100 }, // 1725
                new Abonent { Id = 1, Square = 90 },  // 1552.5
                new Abonent { Id = 2, Square = 80 },  // 1380
                new Abonent { Id = 3, Square = 70 },  // 1207.5
                new Abonent { Id = 4, Square = 60 },  // 1035 
            };

            // 1) ругаемся, была сумма 1380, теперь должна быть 1725, т.к. подключаем двух абонентов с большими площадями
            var deltaAbonents = new List<AbonentOnServiceRequest>
            {
                new AbonentOnServiceRequest { AbonentId = 0, Checked = true },
                new AbonentOnServiceRequest { AbonentId = 1, Checked = true },
            };

            var connectedServices = new List<ConnectedService>
            {
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[0], Dt = new DateTime (2023, 10, 1), IsActive = false },
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[1], Dt = new DateTime (2023, 10, 1), IsActive = false },
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[2], Dt = new DateTime (2023, 10, 1), IsActive = true },
            };

            var minAmount = OsiServiceLogic.CheckDeltaAbonentsForMinAmountForBigRepairService(mrp, bigRepairMrpPercent, 1380, deltaAbonents, abonents, connectedServices);
            Assert.Equal(1725, minAmount);


            // 2) не ругаемся, была сумма 1725, теперь упала до 1380, но минимум соблюден
            // отключаем двух абонентов с большими площадями
            deltaAbonents = new List<AbonentOnServiceRequest>
            {
                new AbonentOnServiceRequest { AbonentId = 0, Checked = false },
                new AbonentOnServiceRequest { AbonentId = 1, Checked = false },
            };

            connectedServices = new List<ConnectedService>
            {
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[0], Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[1], Dt = new DateTime (2023, 10, 1), IsActive = true },
                new ConnectedService { OsiServiceId = 1, Abonent = abonents[2], Dt = new DateTime (2023, 10, 1), IsActive = true },
            };

            minAmount = OsiServiceLogic.CheckDeltaAbonentsForMinAmountForBigRepairService(mrp, bigRepairMrpPercent, 1725, deltaAbonents, abonents, connectedServices);
            Assert.Equal(0, minAmount);
        }
    }
}
