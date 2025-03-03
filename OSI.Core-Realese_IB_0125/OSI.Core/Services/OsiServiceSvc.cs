using Microsoft.EntityFrameworkCore;
using OSI.Core.Logic;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using OSI.Core.Models.Responses;
using OSI.Core.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IOsiServiceSvc
    {
        //errors
        string GetExceptionMessage(Exception ex);

        //check
        Task CheckOsiServiceById(int id);

        //get
        Task<OsiService> GetOsiServiceById(int id);
        Task<IEnumerable<OsiService>> GetOsiServicesByOsiId(int osiId);

        //crud
        Task<OsiService> AddOrUpdateOsiService(int id, OsiServiceRequest request);
        //Task DeleteOsiService(int id);

        Task<List<ServiceGroupResponse>> GetServiceGroupsInfo(int osiId);
        Task SetOsiServiceAbonents(int id, List<AbonentOnServiceRequest> abonents);
        Task AddAbonentInAllServices(Abonent abonent);
        Task RemoveAbonentInAllServices(Abonent abonent);

        Task SetStateForService(int osiServiceId, bool isActive, string note = null);

        //Task<OsiServiceResponse> GetOsiServiceByIdExtended(int id);

        Task<List<AbonentOnServiceResponse>> GetOsiServiceAbonents(int osiServiceId);

        //Task<List<ServiceByAbonentResponse>> GetServicesByAbonent(int osiId, int abonentId, int groupId);
        Task<dynamic> GetGroupAndServicesForFixes(int osiId);
    }

    public class OsiServiceSvc : IOsiServiceSvc
    {
        #region Конструктор
        private readonly IOsiSvc osiSvc;
        private readonly ICatalogSvc catalogSvc;

        public OsiServiceSvc(IOsiSvc osiSvc, ICatalogSvc catalogSvc)
        {
            this.osiSvc = osiSvc;
            this.catalogSvc = catalogSvc;
        }
        #endregion

        public string GetExceptionMessage(Exception ex)
        {
            string message = ex.Message;
            if (ex is DbUpdateException)
            {
                if (ex.InnerException.Message.IndexOf("duplicate key") > -1)
                {
                    //if (ex.InnerException.Message.IndexOf("unq_osi_services1") > -1)
                    //    message = "Такая услуга уже есть на данном ОСИ";
                }
                else if (/*ex.InnerException.Message.IndexOf("fk_plan_accural_services_osi_serv") > -1 ||*/ ex.InnerException.Message.IndexOf("fk_transactions_osiservices") > -1)
                {
                    message = "Данная услуга уже используется в операциях начислений";
                }
            }
            return message;
        }

        public async Task CheckOsiServiceById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (!await db.OsiServices.AnyAsync(o => o.Id == id))
                throw new Exception("Услуга ОСИ не найдена");
        }

        public async Task<IEnumerable<OsiService>> GetOsiServicesByOsiId(int osiId)
        {
            await osiSvc.CheckOsiById(osiId);
            using var db = OSIBillingDbContext.DbContext;
            var osiServices = await db.OsiServices.Include(s => s.ServiceGroup)
                .Where(s => s.OsiId == osiId).ToListAsync();
            return osiServices;
        }

        public async Task<OsiService> GetOsiServiceById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var osiService = await db.OsiServices.Include(s => s.ServiceGroup)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (osiService == null)
                throw new Exception("Услуга ОСИ не найдена");

            return osiService;
        }

        public async Task DeleteOsiService(int id)
        {
            OsiService osiService = await GetOsiServiceById(id);
            if (osiService.IsOsibilling)
                throw new Exception("Данную услугу удалять нельзя");

            using var db = OSIBillingDbContext.DbContext;
            db.OsiServices.Remove(osiService);
            await db.SaveChangesAsync();
        }

        public async Task<OsiService> AddOrUpdateOsiService(int id, OsiServiceRequest request)
        {
            var osi = await osiSvc.GetOsiById(request.OsiId);

            OsiService model = id != default ? await GetOsiServiceById(id) : new OsiService
            {
                IsActive = true,
                IsOsibilling = false
            };

            using var db = OSIBillingDbContext.DbContext;

            // 10-09-2022, shuma, теперь проверяется только у активных услуг (OSI-171)
            var osiServices = await db.OsiServices.Where(o => o.OsiId == request.OsiId && o.IsActive).ToListAsync();

            ServiceGroup serviceGroup = await db.ServiceGroups
                .Include(g => g.AccountType)
                .Include(g => g.AllowedAccuralMethods).ThenInclude(a => a.AccuralMethod)
                .Include(g => g.ServiceNameExamples)
                .FirstOrDefaultAsync(g => g.Id == request.ServiceGroupId) ?? throw new Exception("Группа с Id = " + request.ServiceGroupId + " не найдена");

            var osiAccounts = await db.OsiAccounts.Where(o => o.OsiId == request.OsiId).ToListAsync();

            var accuralMethod = await db.AccuralMethods.FirstOrDefaultAsync(o => o.Id == request.AccuralMethodId) ?? throw new Exception("Метод начисления с Id = " + request.AccuralMethodId + " не найден");

            var connectedServices = id != default ? await db.ConnectedServices.Include(cs => cs.Abonent).Where(a => a.OsiServiceId == id).ToListAsync() : null;

            string errorMessage = OsiServiceLogic.CheckAddOrUpdateConditions(id, Startup.MRP, request, osi, osiServices, serviceGroup, osiAccounts, accuralMethod, connectedServices);
            if (errorMessage != "") throw new Exception(errorMessage);

            model.NameRu = request.NameRu;
            model.NameKz = request.NameKz;
            model.Osi = null;
            model.OsiId = request.OsiId;
            model.ServiceGroup = null;
            model.ServiceGroupId = request.ServiceGroupId;

            if (id == default) db.OsiServices.Add(model);
            else db.OsiServices.Update(model);

            await db.SaveChangesAsync();

            // сумма
            await PeriodicDataLogic.SaveOsiServiceAmount(db, new OsiServiceAmount
            {
                Amount = request.Amount,
                Dt = DateTime.Today,
                OsiId = model.OsiId,
                OsiServiceId = model.Id,
                AccuralMethodId = request.AccuralMethodId,
                Note = id == default ? "добавление услуги" : "изменение услуги"
            }, true);

            // обновим модель для подгрузки связок
            return id == default ? await GetOsiServiceById(model.Id) : null;
        }

        /// <summary>
        /// отключение/включение услуги всем абонентам
        /// </summary>
        /// <param name="osiServiceId"></param>
        /// <param name="isActive"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        public async Task SetStateForService(int osiServiceId, bool isActive, string note = null)
        {
            using var db = OSIBillingDbContext.DbContext;
            OsiService osiService = await db.OsiServices.FirstOrDefaultAsync(a => a.Id == osiServiceId);
            if (osiService == null) throw new Exception("Услуга ОСИ не найдена");
            var abonents = await db.Abonents.Where(o => o.OsiId == osiService.OsiId && o.IsActive).ToListAsync();
            var parkingPlaces = db.ParkingPlaces.Where(a => a.OsiServiceId == osiService.Id);
            var parkingServiceGroup = await db.ServiceGroups.FirstOrDefaultAsync(s => s.Code == "PARKING");
            foreach (var abonent in abonents)
            {
                bool active = isActive;
                // парковка
                if (osiService.ServiceGroupId == parkingServiceGroup.Id)
                {
                    active = parkingPlaces?.Any(p => p.AbonentId == abonent.Id) ?? false;
                }

                await PeriodicDataLogic.SaveConnectedService(db, new ConnectedService
                {
                    AbonentId = abonent.Id,
                    OsiId = osiService.OsiId,
                    OsiServiceId = osiService.Id,
                    Dt = DateTime.Today,
                    IsActive = active,
                    Note = note
                });
            }
            osiService.IsActive = isActive;
            db.OsiServices.Update(osiService);
            await db.SaveChangesAsync();
        }

        public async Task<List<ServiceGroupResponse>> GetServiceGroupsInfo(int osiId)
        {
            await osiSvc.CheckOsiById(osiId);
            var list = new List<ServiceGroupResponse>();
            using var db = OSIBillingDbContext.DbContext;

            var serviceGroups = await db.ServiceGroups
                .Include(g => g.ServiceNameExamples)
                .Include(g => g.AllowedAccuralMethods)
                .ThenInclude(a => a.AccuralMethod)
                .ToListAsync();
            var abonents = await db.Abonents.Where(o => o.OsiId == osiId && o.IsActive).ToListAsync();
            // 10-09-2022, shuma, теперь только активные услуги (OSI-171)
            var osiServices = await db.OsiServices
                .Include(o => o.ConnectedServices)
                .Include(o => o.OsiServiceAmounts)
                .Where(o => o.OsiId == osiId && o.IsActive)
                .ToListAsync();
            var lastPlan = await PlanAccuralLogic.GetLastPlanAccuralByOsiIdOrCreateNew(osiId);

            foreach (var group in serviceGroups.OrderBy(a => a.Id))
            {
                var serviceGroupResponse = new ServiceGroupResponse
                {
                    Id = group.Id,
                    GroupNameRu = group.NameRu,
                    GroupNameKz = group.NameKz,
                    CanChangeName = group.CanChangeName,
                    JustOne = group.JustOne,
                    CanEditAbonents = group.CanEditAbonents,
                    CanCreateFixes = group.CanCreateFixes,
                    AccuralMethods = group.AllowedAccuralMethods.Select(a => a.AccuralMethod).ToList(),
                    ServiceNameExamples = group.ServiceNameExamples.ToList(),
                    Services = new List<OsiServiceResponse>()
                };

                foreach (var osiService in osiServices.Where(o => o.ServiceGroupId == group.Id).OrderBy(o => o.NameRu))
                {
                    // пропускаем услугу Osi Billing, если включена галочка
                    if ((lastPlan?.UssikingIncluded ?? true) && osiService.IsOsibilling)
                        continue;

                    int countActiveAbonents = 0;
                    foreach (var abonent in abonents)
                    {
                        var abonentConnecting = osiService.ConnectedServices.OrderByDescending(o => o.Dt).FirstOrDefault(a => a.AbonentId == abonent.Id);
                        if (abonentConnecting?.IsActive ?? false)
                        {
                            countActiveAbonents++;
                        }
                    }
                    var osiServiceAmount = osiService.OsiServiceAmounts.OrderByDescending(o => o.Dt).FirstOrDefault();
                    var osiServiceResponse = new OsiServiceResponse
                    {
                        Id = osiService.Id,
                        NameRu = osiService.NameRu,
                        NameKz = osiService.NameKz,
                        ServiceGroupId = osiService.ServiceGroupId,
                        AccuralMethodId = osiServiceAmount?.AccuralMethodId ?? 0,
                        Amount = osiServiceAmount?.Amount ?? 0,
                        IsOsiBilling = osiService.IsOsibilling,
                        IsActive = osiService.IsActive,
                        CountAllAbonents = abonents.Count,
                        CountActiveAbonents = countActiveAbonents
                    };
                    serviceGroupResponse.Services.Add(osiServiceResponse);
                }
                list.Add(serviceGroupResponse);
            }

            return list;
        }

        public async Task<List<AbonentOnServiceResponse>> GetOsiServiceAbonents(int osiServiceId)
        {
            using var db = OSIBillingDbContext.DbContext;
            var osiService = await db.OsiServices.Include(s => s.ServiceGroup).FirstOrDefaultAsync(s => s.Id == osiServiceId) ?? throw new Exception("Услуга ОСИ не найдена");
            var parkingPlaces = await db.ParkingPlaces.Where(a => a.OsiServiceId == osiService.Id).ToListAsync();
            var abonents = await db.Abonents.Where(o => o.OsiId == osiService.OsiId && o.IsActive).ToListAsync();
            var areaTypes = await db.AreaTypes.ToListAsync();

            List<AbonentOnServiceResponse> osiServiceAbonents = new List<AbonentOnServiceResponse>();

            // OSI-354, правила отбора абонентов по Кап.ремонту отличаются от остальных услуг
            if (osiService.ServiceGroup.Code == "BIG_REPAIR")
            {
                // подключения берутся по группе в целом
                // OSI-620, услуга должна быть активной
                var connectedServices = await db.ConnectedServices
                    .Where(a => a.OsiId == osiService.OsiId)
                    .Where(a => a.OsiService.ServiceGroupId == osiService.ServiceGroupId)
                    .Where(a => a.OsiService.IsActive)
                    .ToListAsync();
                osiServiceAbonents = OsiServiceLogic.GetOsiServiceAbonentsBigRepair(osiService, connectedServices, parkingPlaces, abonents, areaTypes);                
            }
            else
            {
                var connectedServices = await db.ConnectedServices.Where(a => a.OsiServiceId == osiService.Id).ToListAsync();
                osiServiceAbonents = OsiServiceLogic.GetOsiServiceAbonentsOtherServices(osiService, connectedServices, parkingPlaces, abonents, areaTypes);
            }
            return osiServiceAbonents;
        }

        public async Task SetOsiServiceAbonents(int id, List<AbonentOnServiceRequest> deltaAbonents)
        {
            var osiService = await GetOsiServiceById(id);
            var osi = await osiSvc.GetOsiById(osiService.OsiId);
            using var db = OSIBillingDbContext.DbContext;
            using var dbTransaction = await db.Database.BeginTransactionAsync();
            try
            {
                var parkingServiceGroup = await db.ServiceGroups.FirstOrDefaultAsync(s => s.Code == "PARKING");

                if (osiService.ServiceGroup.Code == "BIG_REPAIR")
                {
                    var osiServiceAmount = await db.OsiServiceAmounts
                        .Include(a => a.AccuralMethod)
                        .OrderByDescending(o => o.Dt)
                        .FirstOrDefaultAsync(a => a.OsiServiceId == osiService.Id);
                    
                    // 2 пункт задачи OSI-354
                    if (osiServiceAmount.AccuralMethod.Code == "FIX_SUM_FLAT")
                    {
                        var connectedServices = await db.ConnectedServices
                            .Include(cs => cs.Abonent)
                            .Where(a => a.OsiServiceId == id)
                            .ToListAsync();

                        var abonents = await db.Abonents.Where(a => a.OsiId == osiService.OsiId && a.IsActive).ToListAsync();

                        var minAmount = OsiServiceLogic.CheckDeltaAbonentsForMinAmountForBigRepairService(Startup.MRP, osi.BigRepairMrpPercent, osiServiceAmount.Amount, deltaAbonents, abonents, connectedServices);
                        if (minAmount > 0)
                        {
                            string minAmountStr = minAmount.ToString("F2").Replace(",", ".") + " тг";
                            throw new Exception($"Минимальный тариф с учетом выбранных помещений {minAmountStr}. Сначала установите тариф {minAmountStr}, затем добавьте помещение");
                        }
                    }
                }

                foreach (var a in deltaAbonents)
                {
                    await PeriodicDataLogic.SaveConnectedService(db, new ConnectedService
                    {
                        AbonentId = a.AbonentId,
                        OsiId = osiService.OsiId,
                        OsiServiceId = osiService.Id,
                        Dt = DateTime.Today,
                        IsActive = a.Checked
                    });

                    // парковка
                    if (osiService.ServiceGroupId == parkingServiceGroup.Id)
                    {
                        await PeriodicDataLogic.SaveParkingPlaces(db, new ParkingPlace
                        {
                            AbonentId = a.AbonentId,
                            OsiId = osiService.OsiId,
                            OsiServiceId = osiService.Id,
                            Dt = DateTime.Today,
                            Places = a.ParkingPlaces
                        });
                    }
                }

                await db.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task AddAbonentInAllServices(Abonent abonent)
        {
            using var db = OSIBillingDbContext.DbContext;
            using var dbTransaction = await db.Database.BeginTransactionAsync();
            try
            {
                var osiServices = await db.OsiServices.Include(s => s.ServiceGroup).Where(o => o.OsiId == abonent.OsiId).ToListAsync();
                foreach (var osiService in osiServices)
                {
                    bool addNewAbonent = false;
                    switch (osiService.ServiceGroup.Code)
                    {
                        case "LIFT":
                            addNewAbonent = abonent.Floor >= 2;
                            break;
                        case "PARKING":
                            addNewAbonent = false;
                            break;
                        default:
                            addNewAbonent = true;
                            break;
                    }
                    if (addNewAbonent)
                    {
                        await PeriodicDataLogic.SaveConnectedService(db, new ConnectedService
                        {
                            AbonentId = abonent.Id,
                            OsiId = osiService.OsiId,
                            OsiServiceId = osiService.Id,
                            Dt = DateTime.Today,
                            IsActive = true
                        });
                    }
                }
                await db.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task RemoveAbonentInAllServices(Abonent abonent)
        {
            using var db = OSIBillingDbContext.DbContext;
            using var dbTransaction = await db.Database.BeginTransactionAsync();
            try
            {
                var osiServices = await db.OsiServices.Include(s => s.ServiceGroup).Where(o => o.OsiId == abonent.OsiId).ToListAsync();
                foreach (var osiService in osiServices)
                {
                    await PeriodicDataLogic.SaveConnectedService(db, new ConnectedService
                    {
                        AbonentId = abonent.Id,
                        OsiId = osiService.OsiId,
                        OsiServiceId = osiService.Id,
                        Dt = DateTime.Today,
                        IsActive = false
                    });
                }
                await db.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        //public async Task<List<ServiceByAbonentResponse>> GetServicesByAbonent(int osiId, int abonentId, int groupId)
        //{
        //    using var db = OSIBillingDbContext.DbContext;
        //    var osiServices = await db.OsiServices.Where(o => o.OsiId == osiId && o.ServiceGroupId == groupId && !o.IsOsibilling).ToListAsync();
        //    var groupTargetPay = await db.ServiceGroups.FirstOrDefaultAsync(g => g.Code == "TARGET_PAY");
        //    if (groupTargetPay == null)
        //    {
        //        throw new Exception("Не найдена группа \"Разовый целевой взнос\"");
        //    }

        //    var list = new List<ServiceByAbonentResponse>();
        //    foreach (var osiService in osiServices)
        //    {
        //        var connectedServices = db.ConnectedServices.Where(a => a.AbonentId == abonentId && a.OsiServiceId == osiService.Id).OrderByDescending(o => o.Dt).ToList();
        //        var abonentConnecting = connectedServices.FirstOrDefault(a => a.AbonentId == abonentId);
        //        if (abonentConnecting != null)
        //        {
        //            // OSI-214 услуги разового целевого взноса должны отражаться всегда, если они были подключены к абоненту однажды
        //            if (abonentConnecting.IsActive || osiService.ServiceGroupId == groupTargetPay.Id)
        //            {
        //                list.Add(new ServiceByAbonentResponse
        //                {
        //                    Id = osiService.Id,
        //                    NameRu = osiService.NameRu,
        //                    NameKz = osiService.NameKz
        //                });
        //            }
        //        }
        //    }

        //    return list;
        //}

        /// <summary>
        /// Выдает всевозможные группы и сервисы, которые когда-либо были на данном ОСИ, для создания корректировок
        /// </summary>
        /// <param name="osiId">Оси ID</param>
        /// <returns></returns>
        public async Task<dynamic> GetGroupAndServicesForFixes(int osiId)
        {
            using var db = OSIBillingDbContext.DbContext;
            var osiServicesGroupsAndServices = await db.OsiServices
                .Include(s => s.ServiceGroup)
                .Where(o => o.OsiId == osiId && !o.IsOsibilling && o.ConnectedServices.Any())
                .ToListAsync();

            var groups = new List<dynamic>();
            foreach (var g in osiServicesGroupsAndServices
                .GroupBy(o => new { o.ServiceGroupId, o.ServiceGroup.NameRu, o.ServiceGroup.NameKz })
                .OrderBy(g => g.Key.ServiceGroupId))
            {
                groups.Add(new
                {
                    group_id = g.Key.ServiceGroupId,
                    nameRu = g.Key.NameRu,
                    nameKz = g.Key.NameKz ?? g.Key.NameRu,
                    services = g.Select(a => new
                    {
                        service_id = a.Id,
                        nameRu = a.NameRu,
                        nameKz = a.NameKz ?? a.NameRu,
                    })
                });
            }

            return groups;
        }
    }
}
