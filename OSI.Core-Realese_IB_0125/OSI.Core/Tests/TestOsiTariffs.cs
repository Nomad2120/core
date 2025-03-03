using OSI.Core.Logic;
using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OSI.Core.Tests
{
    public class TestOsiTariffs
    {
        [Fact]
        public void TestTariffs()
        {
            List<OsiTariff> osiTariffs = new List<OsiTariff>
            {
                new OsiTariff { Dt = new DateTime(2022, 04, 30), Value = 53 },
                new OsiTariff { Dt = new DateTime(2022, 04, 1), Value = 50 },
                new OsiTariff { Dt = new DateTime(2022, 04, 12), Value = 52 },
                new OsiTariff { Dt = new DateTime(2022, 04, 11), Value = 51 },
            };
            decimal value = OsiTariffLogic.GetOsiTariffValueFromListByDate(osiTariffs, new DateTime(2022, 04, 3));
            Assert.Equal(50, value);
            value = OsiTariffLogic.GetOsiTariffValueFromListByDate(osiTariffs, new DateTime(2022, 04, 11));
            Assert.Equal(51, value);
            value = OsiTariffLogic.GetOsiTariffValueFromListByDate(osiTariffs, new DateTime(2022, 04, 13));
            Assert.Equal(52, value);
            value = OsiTariffLogic.GetOsiTariffValueFromListByDate(osiTariffs, new DateTime(2022, 04, 23));
            Assert.Equal(52, value);
            value = OsiTariffLogic.GetOsiTariffValueFromListByDate(osiTariffs, new DateTime(2022, 05, 1));
            Assert.Equal(53, value);
            value = OsiTariffLogic.GetOsiTariffValueFromListByDate(osiTariffs, new DateTime(2022, 03, 1));
            Assert.Equal(Services.TariffSvc.DefaultTariff, value);

            osiTariffs = null;
            value = OsiTariffLogic.GetOsiTariffValueFromListByDate(osiTariffs, new DateTime(2022, 05, 1));
            Assert.Equal(Services.TariffSvc.DefaultTariff, value);
        }
    }
}
