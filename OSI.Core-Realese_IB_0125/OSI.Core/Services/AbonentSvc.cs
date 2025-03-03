using AutoMapper;
using Blazorise;
using ESoft.CommonLibrary;
using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using OSI.Core.Models.Requests;
using System;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IAbonentSvc
    {
        string GetExceptionMessage(Exception ex);

        // get & check
        Task<Abonent> GetAbonentById(int id);
        Task<Abonent> GetAbonentForPaymentService(string abonentNum);
        Task CheckAbonentById(int id);

        // crud
        Task<Abonent> AddAbonent(AbonentRequest request);
        Task UpdateAbonent(int id, AbonentRequest request);
        Task DeleteAbonent(int id);

        // other
        Task CheckHistoryAndCreateNew(Abonent abonent, DateTime newDate);

        Task<Osi> GetOsiByAbonentId(int id);

        Task<Arendator> AddArendator(ArendatorRequest arendatorRequest);
        Task SetStatusForAbonent(int id, bool status);
    }

    public class AbonentSvc : IAbonentSvc
    {
        private readonly IOsiSvc osiSvc;
        private readonly IPlanAccuralSvc planAccuralSvc;
        private readonly IOsiServiceSvc osiServiceSvc;
        private readonly IMapper mapper;

        public AbonentSvc(IOsiSvc osiSvc, IPlanAccuralSvc planAccuralSvc, IOsiServiceSvc osiServiceSvc, IMapper mapper)
        {
            this.osiSvc = osiSvc;
            this.planAccuralSvc = planAccuralSvc;
            this.osiServiceSvc = osiServiceSvc;
            this.mapper = mapper;
        }

        public string GetExceptionMessage(Exception ex)
        {
            string message = ex.Message;
            if (ex is DbUpdateException)
            {
                if (ex.InnerException.Message.IndexOf("fk_osi_service_saldo_abonents") > -1)
                {
                    message = "По данному абоненту указано начальное сальдо по услуге. Сначала удалите сальдо";
                }
                else if (ex.InnerException.Message.IndexOf("fk_transactions_abonents") > -1)
                {
                    message = "По данному абоненту уже проводились операции";
                }
                else if (ex.InnerException.Message.IndexOf("abonents_uk") > -1)
                {
                    message = "Помещение с такими данными уже есть";
                }
                else if (ex.InnerException.Message.IndexOf("violates foreign key constraint") > -1)
                {
                    message = "Данный абонент не может быть изменен или удален, т.к. его данные уже используются";
                }
            }
            return message;
        }

        public async Task<Abonent> GetAbonentById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var abonent = await db.Abonents.Include(a => a.AreaType).FirstOrDefaultAsync(a => a.Id == id);
            if (abonent == null)
                throw new Exception("Абонент не найден");

            return abonent;
        }

        public async Task<Abonent> GetAbonentForPaymentService(string abonentNum)
        {
            using var db = OSIBillingDbContext.DbContext;
            Abonent abonent = null;
            IQueryable<Abonent> query = db.Abonents.Include(a => a.AreaType).Include(a => a.Osi);
            if (int.TryParse(abonentNum, out int id))
            {
                abonent = await query.FirstOrDefaultAsync(a => a.Id == id);
            }
            if (abonent == null)
            {
                abonent = await query.FirstOrDefaultAsync(a => a.ErcAccount == abonentNum);
            }
            if (abonent == null)
                throw new Exception("Абонент не найден");

            if (!abonent.Osi.IsLaunched
                && !abonent.OsiId.In(272, 273, 276, 348, 234)) // OSI-323, OSI-347, OSI-355, OSI-591
                throw new Exception("ОСИ не активно");

            await SetAbonentData(abonent, db);

            return abonent;
        }

        private static async Task SetAbonentData(Abonent abonent, OSIBillingDbContext db)
        {
            if (abonent is null)
            {
                throw new ArgumentNullException(nameof(abonent));
            }

            if (db is null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            var osi = abonent.Osi;

            abonent.OsiName = osi.Name;

            var address = osi.Address + ", кв. " + abonent.Flat;
            if (abonent.AreaTypeCode == AreaTypeCodes.NON_RESIDENTIAL || abonent.AreaTypeCode == AreaTypeCodes.BASEMENT)
            {
                if (await db.Abonents.AnyAsync(z => z.OsiId == abonent.OsiId && z.Flat == abonent.Flat && z.AreaTypeCode == AreaTypeCodes.RESIDENTIAL && z.IsActive))
                {
                    address += abonent.AreaTypeCode switch
                    {
                        AreaTypeCodes.NON_RESIDENTIAL => "Н",
                        AreaTypeCodes.BASEMENT => "П",
                        _ => ""
                    };
                }
            }
            abonent.Address = address;

            var planAccuralDates = await db.PlanAccurals
                                        .Where(pa => pa.OsiId == abonent.OsiId && pa.AccuralCompleted)
                                        .OrderBy(pa => pa.AccuralDate == null)
                                        .ThenByDescending(pa => pa.AccuralDate)
                                        .ThenByDescending(pa => pa.BeginDate)
                                        .Select(pa => new { pa.BeginDate, pa.AccuralDate })
                                        .FirstOrDefaultAsync();
            DateTime invoiceDate = planAccuralDates?.AccuralDate
                            ?? planAccuralDates?.BeginDate
                            ?? await db.Osies
                            .Include(o => o.Registration)
                            .Where(o => o.Id == abonent.OsiId)
                            .Select(o => o.Registration.CreateDt)
                            .FirstAsync();
            var invoiceNum = invoiceDate.ToString("yyMMdd");
            invoiceNum += abonent.Id.ToString().PadLeft(6, '0')[^6..];
            abonent.InvoiceNum = invoiceNum;
        }

        public async Task CheckAbonentById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (!await db.Abonents.AnyAsync(o => o.Id == id))
                throw new Exception("Абонент не найден");
        }

        public async Task RecalcAbonents(Osi osi)
        {
            using var db = OSIBillingDbContext.DbContext;
            int count = await db.Abonents.CountAsync(a => a.OsiId == osi.Id && !a.External && a.IsActive);
            osi.ApartCount = count;
            db.Osies.Update(osi);
            await db.SaveChangesAsync();
        }

        //private void CopyValuesFromRequestToModel(Abonent model, AbonentRequest request)
        //{
        //    model.Osi = null;
        //    model.AreaType = null;
        //    model.AreaTypeCode = request.AreaTypeCode;
        //    model.Flat = request.Flat;
        //    model.Floor = request.Floor;
        //    model.Idn = request.Idn;
        //    model.Name = request.Name;
        //    model.OsiId = request.OsiId;
        //    model.Phone = request.Phone;
        //    model.Square = request.Square;
        //    model.LivingFact = request.LivingFact;
        //    model.LivingJur = request.LivingJur;
        //    model.Owner = request.Owner;
        //    model.External = request.External;
        //    model.EffectiveSquare = request.EffectiveSquare;
        //}

        public async Task<string> ProcessAbonentUkConstraint(int osiId, string flat, AreaTypeCodes areaType, bool external)
        {
            using var db = OSIBillingDbContext.DbContext;

            // OSI-366: проверяем по CONSTRAINT abonents_uk UNIQUE (osi_id, flat, area_type_code, external) 
            var exist = await db.Abonents
                .Include(a => a.AreaType)
                .Where(a => a.OsiId == osiId)
                .Where(a => a.Flat == flat)
                .Where(a => a.AreaTypeCode == areaType)
                .Where(a => a.External == external)
                .FirstOrDefaultAsync() ?? throw new Exception("Не найдено помещение по заданным параметрам"); // такой вариант не должен случиться

            return exist.IsActive
                ? (external
                    ? "Арендатор с таким номером помещения у вас уже существует, введите другой номер помещения"
                    : $"Помещение с номером {exist.Flat} и типом {exist.AreaTypeNameRu} уже есть, введите другой номер")
                : (external
                    ? "Арендатор с таким номером помещения у вас уже существует, но он деактивирован. Если нужно активировать, свяжитесь с оператором, либо введите другой номер помещения."
                    : $"Помещение с номером {exist.Flat} и типом {exist.AreaTypeNameRu} уже есть, но оно деактивировано. Если нужно активировать, свяжитесь с оператором, либо введите другой номер помещения.");
        }

        public async Task<Abonent> AddAbonent(AbonentRequest request)
        {
            try
            {
                Osi osi = await osiSvc.GetOsiById(request.OsiId);
                if (request.Floor > osi.Floors)
                    throw new Exception("Максимальный этаж: " + osi.Floors);

                using var db = OSIBillingDbContext.DbContext;
                Abonent abonent = mapper.Map<Abonent>(request); // используем автомаппер для создания нового объекта
                abonent.IsActive = true;
                db.Abonents.Add(abonent);
                await db.SaveChangesAsync();
                //await planAccuralSvc.AddOrRemovePlanAccuralAbonentInLastPlan(abonent);
                //await osiServiceSvc.AddAbonentInAllServices(abonent);
                await RecalcAbonents(osi);
                return abonent;
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException.Message.IndexOf("abonents_uk") > -1)
                {
                    string error = await ProcessAbonentUkConstraint(request.OsiId, request.Flat, request.AreaTypeCode, false); // проверяем именно false, т.к. фронт здесь не передает External вообще
                    throw new Exception(error);
                }
                else throw;
            }
        }

        public async Task UpdateAbonent(int id, AbonentRequest request)
        {
            try
            {
                Osi osi = await osiSvc.GetOsiById(request.OsiId);
                if (request.Floor > osi.Floors)
                    throw new Exception("Максимальный этаж: " + osi.Floors);

                using var db = OSIBillingDbContext.DbContext;
                Abonent abonent = await db.Abonents.FirstOrDefaultAsync(a => a.Id == id);
                if (abonent == null)
                    throw new Exception("Абонент не найден");

                // OSI-393, проверка абонента на участие в кап.ремонте, где сумма услуги зависит от максимальной площади всех абонентов
                if (abonent.Square != request.Square)
                {
                    var connectedBigRepairService = await db.ConnectedServices
                        .OrderByDescending(a => a.Id)
                        .FirstOrDefaultAsync(a => a.AbonentId == abonent.Id && a.OsiService.ServiceGroupId == 2);

                    if (connectedBigRepairService?.IsActive ?? false)
                    {
                        var osiServiceAmount = await db.OsiServiceAmounts
                            .OrderByDescending(a => a.Id)
                            .FirstOrDefaultAsync(a => a.OsiServiceId == connectedBigRepairService.OsiServiceId);

                        if (osiServiceAmount != null && osiServiceAmount.AccuralMethodId == 2) // только для фиксированной суммы с помещения
                        {
                            decimal requiredAmount = request.Square * Startup.MRP * osi.BigRepairMrpPercent;
                            if (requiredAmount > 0 && osiServiceAmount.Amount < requiredAmount)
                            {
                                string requiredAmountStr = requiredAmount.ToString("F2").Replace(",", ".") + " тг";
                                throw new Exception($"Для площади {request.Square} минимальный тариф на капитальный ремонт должен быть {requiredAmountStr}. Чтобы изменить площадь, сначала измените тариф на услуге кап.ремонт, в которую входит это помещение");
                            }
                        }
                    }
                }

                abonent = mapper.Map(request, abonent); // используем автомаппер для редактирования существующего объекта

                db.Entry(abonent).State = EntityState.Modified;
                //db.Abonents.Update(abonent);

                // проверяем были ли изменения и добавляем в историю
                var lastHistory = await db.AbonentHistories.OrderByDescending(a => a.Dt).LastOrDefaultAsync(a => a.AbonentId == abonent.Id);
                if (lastHistory == null || IsDifferentsExists(lastHistory, abonent))
                {
                    var abonentHistory = new AbonentHistory
                    {
                        AbonentId = abonent.Id,
                        Dt = DateTime.Now
                    };
                    abonentHistory = mapper.Map(request, abonentHistory); // маппим остальные свойства
                    db.AbonentHistories.Add(abonentHistory);
                }

                await db.SaveChangesAsync();

                //await planAccuralSvc.AddOrRemovePlanAccuralAbonentInLastPlan(abonent);
                //await osiServiceSvc.RemoveAbonentInAllServices(abonent);
                //await RecalcAbonents(osi);
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException.Message.IndexOf("abonents_uk") > -1)
                {
                    string error = await ProcessAbonentUkConstraint(request.OsiId, request.Flat, request.AreaTypeCode, false); // проверяем именно false, т.к. фронт здесь не передает External вообще
                    throw new Exception(error);
                }
                else throw;
            }
        }

        public async Task DeleteAbonent(int id)
        {
            Abonent abonent = await GetAbonentById(id);
            Osi osi = await osiSvc.GetOsiById(abonent.OsiId);
            using var db = OSIBillingDbContext.DbContext;
            db.Abonents.Remove(abonent);
            await db.SaveChangesAsync();
            await RecalcAbonents(osi);
        }

        public async Task SetStatusForAbonent(int id, bool status)
        {
            using var db = OSIBillingDbContext.DbContext;
            Abonent abonent = await db.Abonents.FirstOrDefaultAsync(a => a.Id == id);
            if (abonent == null)
                throw new Exception("Абонент не найден");

            abonent.IsActive = status;
            db.Entry(abonent).Property(a => a.IsActive).IsModified = true;
            await db.SaveChangesAsync();
        }

        private bool IsDifferentsExists(AbonentHistory abonentHistory, Abonent abonent)
        {
            bool r = (abonentHistory.AreaTypeCode != abonent.AreaTypeCode
                ||
                abonentHistory.Flat != abonent.Flat
                ||
                abonentHistory.Floor != abonent.Floor
                ||
                abonentHistory.Idn != abonent.Idn
                ||
                abonentHistory.LivingFact != abonent.LivingFact
                ||
                abonentHistory.LivingJur != abonent.LivingJur
                ||
                abonentHistory.Name != abonent.Name
                ||
                abonentHistory.Phone != abonent.Phone
                ||
                abonentHistory.Square != abonent.Square
                ||
                abonentHistory.EffectiveSquare != abonent.EffectiveSquare
                ||
                abonentHistory.External != abonent.External
                ||
                abonentHistory.Owner != abonent.Owner
                ||
                abonentHistory.OsiId != abonent.OsiId
                ||
                abonentHistory.IsActive != abonent.IsActive
                );

            return r;
        }

        /// <summary>
        /// Проверить данные с последними сохраненными в истории и добавить новые на указанную дату, если были отличия
        /// </summary>
        /// <param name="abonent">Абонент</param>
        /// <param name="newDate">Дата фиксации новых изменений</param>
        /// <returns></returns>
        public async Task CheckHistoryAndCreateNew(Abonent abonent, DateTime newDate)
        {
            using var db = OSIBillingDbContext.DbContext;
            var lastHistory = await db.AbonentHistories.OrderByDescending(a => a.Dt).LastOrDefaultAsync(a => a.AbonentId == abonent.Id);
            if (lastHistory == null || IsDifferentsExists(lastHistory, abonent))
            {
                AbonentHistory abonentHistory = new AbonentHistory
                {
                    AbonentId = abonent.Id,
                    Dt = newDate,
                    AreaTypeCode = abonent.AreaTypeCode,
                    Flat = abonent.Flat,
                    Floor = abonent.Floor,
                    Idn = abonent.Idn,
                    LivingFact = abonent.LivingFact,
                    LivingJur = abonent.LivingJur,
                    Name = abonent.Name,
                    Phone = abonent.Phone,
                    Square = abonent.Square,
                    External = abonent.External,
                    IsActive = abonent.IsActive,
                };
                db.AbonentHistories.Add(abonentHistory);
                await db.SaveChangesAsync();
            }
        }

        public async Task<Osi> GetOsiByAbonentId(int id)
        {
            Abonent abonent = await GetAbonentById(id);
            Osi osi = await osiSvc.GetOsiById(abonent.OsiId);
            return osi;
        }

        public async Task<Arendator> AddArendator(ArendatorRequest request)
        {
            try
            {
                Abonent abonent = new()
                {
                    External = true,
                    AreaTypeCode = AreaTypeCodes.RESIDENTIAL,
                    Flat = request.Flat,
                    Floor = 1,
                    Phone = request.Phone,
                    LivingFact = 1,
                    LivingJur = 1,
                    Idn = request.Idn,
                    OsiId = request.OsiId,
                    Owner = "Собственник",
                    Square = 0,
                    Name = request.Name,
                    IsActive = true
                };

                using var db = OSIBillingDbContext.DbContext;
                db.Abonents.Add(abonent);
                await db.SaveChangesAsync();

                Arendator arendator = new()
                {
                    AbonentId = abonent.Id,
                    Address = request.Address,
                    Rca = request.Rca
                };
                db.Arendators.Add(arendator);
                await db.SaveChangesAsync();

                return arendator;
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException.Message.IndexOf("abonents_uk") > -1)
                {
                    string error = await ProcessAbonentUkConstraint(request.OsiId, request.Flat, AreaTypeCodes.RESIDENTIAL, true);
                    throw new Exception(error);
                }
                else throw;
            }
        }
    }
}
