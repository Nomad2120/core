using DevExpress.Blazor.Internal;
using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using OSI.Core.Models.Responses;
using OSI.Core.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Logic
{
    public static class OsiServiceLogic
    {
        private static void SaveConnectedService(List<ConnectedService> connectedServices, ConnectedService connectedService)
        {
            var old = connectedServices.OrderByDescending(o => o.Dt).FirstOrDefault(a => a.Abonent.Id == connectedService.Abonent.Id);

            if (old != null)
            {
                if (old.IsActive != connectedService.IsActive)
                {
                    if (old.Dt != connectedService.Dt)
                    {
                        connectedServices.Add(connectedService);
                    }
                    else
                    {
                        old.IsActive = connectedService.IsActive;
                    }
                }
            }
            else
            {
                connectedServices.Add(connectedService);
            }
        }

        public static decimal GetBigRepairMinAmount(decimal mrpValue,
                                                    decimal bigRepairMrpPercent,
                                                    decimal osiServiceAmount,
                                                    List<ConnectedService> connectedServices)
        {
            if (connectedServices?.Any() ?? false)
            {
                // вариант shuma
                //var abonents = connectedServices.Select(a => a.Abonent).Distinct().ToList();
                //var connectedAtNow = abonents.Where(a => connectedServices.OrderByDescending(c => c.Dt).FirstOrDefault(c => c.Abonent.Id == a.Id)?.IsActive ?? false).ToList();
                //var biggestSquare = connectedAtNow.Any() ? connectedAtNow.Max(a => a.Square) : 0;

                // вариант 4254
                var biggestSquare = connectedServices.GroupBy(cs => cs.Abonent.Id).Select(g =>
                {
                    var cs = g.OrderByDescending(cs => cs.Dt).First();
                    return cs.IsActive ? cs.Abonent.Square : 0;
                }).Max();

                var minAmount = biggestSquare * mrpValue * bigRepairMrpPercent;
                if (biggestSquare > 0 && osiServiceAmount < minAmount)
                    return minAmount;
            }

            return 0;
        }

        public static decimal CheckDeltaAbonentsForMinAmountForBigRepairService(decimal mrpValue,
                                                                                decimal bigRepairMrpPercent,
                                                                                decimal osiServiceAmount,
                                                                                List<AbonentOnServiceRequest> deltaAbonents,
                                                                                List<Abonent> abonents,
                                                                                List<ConnectedService> connectedServices)
        {
            foreach (var a in deltaAbonents)
            {
                var abonent = abonents.FirstOrDefault(x => x.Id == a.AbonentId) ?? throw new Exception($"Абонент {a.AbonentId} не найден в данном ОСИ");
                var connectedService = new ConnectedService
                {
                    Dt = DateTime.Now,
                    IsActive = a.Checked,
                    Abonent = new Abonent
                    {
                        Id = a.AbonentId,
                        Square = abonent.Square
                    }
                };

                SaveConnectedService(connectedServices, connectedService);
            }
            var minAmount = GetBigRepairMinAmount(mrpValue, bigRepairMrpPercent, osiServiceAmount, connectedServices);
            return minAmount;
        }

        public static string CheckAddOrUpdateConditions(int id,
                                                        decimal mrpValue,
                                                        OsiServiceRequest request,
                                                        Osi osi,
                                                        List<OsiService> osiServices,
                                                        ServiceGroup serviceGroup,
                                                        List<OsiAccount> osiAccounts,
                                                        AccuralMethod accuralMethod,
                                                        List<ConnectedService> connectedServices)
        {
            if (request.Amount <= 0)
                return "Сумма услуги должна быть больше нуля";

            // 28-10-2022, shuma, OSI-161 
            if (!string.IsNullOrEmpty(request.NameRu) && (osiServices?.Any(a => a.NameRu == request.NameRu && a.Id != id && a.ServiceGroupId == request.ServiceGroupId) ?? false))
                return "Услуга с таким наименованием на русском языке уже есть";

            // 28-10-2022, shuma, OSI-161 
            if (!string.IsNullOrEmpty(request.NameKz) && (osiServices?.Any(a => a.NameKz == request.NameKz && a.Id != id && a.ServiceGroupId == request.ServiceGroupId) ?? false))
                return "Услуга с таким наименованием на казахском языке уже есть";

            // ищем требуемый счет в списке счетов ОСИ
            if (!osiAccounts.Any(a => a.AccountTypeCode == serviceGroup.AccountType.Code))
                return "Для данной услуги требуется указать \"" + serviceGroup.AccountType.NameRu + "\" в списке счетов";

            // OSI-163 (кап.ремонт) нельзя подключить услугу более одного раза
            if (serviceGroup.JustOne)
            {
                if (osiServices?.Any(s => (id == default || s.Id != id) && s.ServiceGroupId == request.ServiceGroupId) ?? false)
                    return "Данный тип услуги уже есть на ОСИ";
            }

            // OSI-163 (кап.ремонт) нельзя ввести свое название 
            if (!serviceGroup.CanChangeName)
            {
                if (!serviceGroup.ServiceNameExamples.Any(a => a.NameRu == request.NameRu))
                    return "Русское наименование должно быть одним из: " + string.Join(',', serviceGroup.ServiceNameExamples.Select(a => a.NameRu));
                if (!serviceGroup.ServiceNameExamples.Any(a => a.NameKz == request.NameKz))
                    return "Казахское наименование должно быть одним из: " + string.Join(',', serviceGroup.ServiceNameExamples.Select(a => a.NameKz));
            }

            // только разрешенные методы начисления
            if (!serviceGroup.AllowedAccuralMethods.Any(a => a.AccuralMethodId == request.AccuralMethodId))
            {
                return "Метод начисления должен быть одним из: " + string.Join(',', serviceGroup.AllowedAccuralMethods.Select(a => a.AccuralMethod.Description));
            }

            // OSI-354 
            if (serviceGroup.Code == "BIG_REPAIR")
            {
                // 1 пункт задачи
                if ((accuralMethod.Code == "TARIF_1KVM" || accuralMethod.Code == "TARIF_1KVM_EFF") && request.Amount < mrpValue * osi.BigRepairMrpPercent)
                    return "Сумма услуги должна быть не меньше " + (mrpValue * osi.BigRepairMrpPercent).ToString("F2").Replace(",", ".") + " тг";

                // 2 пункт задачи
                if (accuralMethod.Code == "FIX_SUM_FLAT")
                {
                    var minAmount = GetBigRepairMinAmount(mrpValue, osi.BigRepairMrpPercent, request.Amount, connectedServices);
                    if (minAmount > 0)
                        return "Сумма услуги должна быть не меньше " + minAmount.ToString("F2").Replace(",", ".") + " тг";
                }
            }

            return "";
        }

        public static List<AbonentOnServiceResponse> GetOsiServiceAbonentsBigRepair(OsiService osiService,
                                                                                    List<ConnectedService> connectedServices,
                                                                                    List<ParkingPlace> parkingPlaces,
                                                                                    List<Abonent> abonents,
                                                                                    List<AreaType> areaTypes)
        {
            var osiServiceAbonents = new List<AbonentOnServiceResponse>();
            foreach (var abonent in abonents)
            {
                // OSI-354, ищем чтобы не было подключений на другую услугу из группы Кап.ремонт
                var connectsToOtherBigRepairs = connectedServices?
                    .Where(a => a.AbonentId == abonent.Id && a.OsiServiceId != osiService.Id)
                    .GroupBy(a => a.OsiServiceId)
                    .Select(g =>
                    {
                        var cs = g.OrderByDescending(z => z.Dt).First();
                        return cs.IsActive;
                    });

                if (connectsToOtherBigRepairs?.All(a => a == false) ?? false)
                {
                    var abonentOnServiceResponse = new AbonentOnServiceResponse(abonent);
                    abonentOnServiceResponse.AreaType = areaTypes?.FirstOrDefault(a => a.Code == abonentOnServiceResponse.AreaTypeCode);
                    var abonentConnecting = connectedServices?.OrderByDescending(o => o.Dt).FirstOrDefault(a => a.AbonentId == abonent.Id && a.OsiServiceId == osiService.Id);
                    abonentOnServiceResponse.Checked = abonentConnecting?.IsActive ?? false;

                    // парковка
                    var parkingPlace = parkingPlaces?.OrderByDescending(o => o.Dt).FirstOrDefault(a => a.AbonentId == abonent.Id);
                    abonentOnServiceResponse.ParkingPlaces = parkingPlace?.Places ?? 0;

                    osiServiceAbonents.Add(abonentOnServiceResponse);
                }
            }

            return osiServiceAbonents;
        }

        public static List<AbonentOnServiceResponse> GetOsiServiceAbonentsOtherServices(OsiService osiService,
                                                                                        List<ConnectedService> connectedServices,
                                                                                        List<ParkingPlace> parkingPlaces,
                                                                                        List<Abonent> abonents,
                                                                                        List<AreaType> areaTypes)
        {
            var osiServiceAbonents = new List<AbonentOnServiceResponse>();
            foreach (var abonent in abonents)
            {
                var abonentOnServiceResponse = new AbonentOnServiceResponse(abonent);
                abonentOnServiceResponse.AreaType = areaTypes?.FirstOrDefault(a => a.Code == abonentOnServiceResponse.AreaTypeCode);
                var abonentConnecting = connectedServices?.OrderByDescending(o => o.Dt).FirstOrDefault(a => a.AbonentId == abonent.Id && a.OsiServiceId == osiService.Id);
                abonentOnServiceResponse.Checked = abonentConnecting?.IsActive ?? false;

                // парковка
                var parkingPlace = parkingPlaces?.OrderByDescending(o => o.Dt).FirstOrDefault(a => a.AbonentId == abonent.Id);
                abonentOnServiceResponse.ParkingPlaces = parkingPlace?.Places ?? 0;

                osiServiceAbonents.Add(abonentOnServiceResponse);
            }

            return osiServiceAbonents;
        }
    }
}
