using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OSI.Core.Comparer;
using OSI.Core.Logic;
using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using OSI.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IOsiSvc
    {
        Task<IEnumerable<Osi>> GetOsies();
        Task<IEnumerable<Osi>> GetActiveOsies();
        Task<Osi> GetOsiById(int id);
        Task CheckOsiById(int id);

        Task UpdateOsi(int id, OsiRequest request, ClaimsPrincipal user);
        Task AddOrUpdateModel(Osi model);
        Task<IEnumerable<Osi>> GetActiveOsiByUserId(int userId);
        Task StartOsi(int id);
        Task StopOsi(int id);
        Task ActivateOsi(int id);
        Task DeactivateOsi(int id);
        Task<IEnumerable<OsiDoc>> GetOsiDocs(int osiId);
        Task<OsiDoc> AddOsiDoc(int osiId, AddScanDoc request);
        Task DeleteOsiDoc(int osiId, int docId);

        Task<IEnumerable<Abonent>> GetAbonentsByOsiId(int osiId, bool onlyExternals = false, bool withDeactivated = false);

        Task SaveWizardStep(int osiId, string wizardStep);

        Task<IEnumerable<Act>> GetActsByOsiId(int osiId, ActStateCodes state);
        Task<Abonent> GetAbonentByOsiIdAndFlat(int osiId, string flat);

        Task<IEnumerable<User>> GetOsiUsers(int osiId);

        //Task<IEnumerable<Act>> GetSignedAndProvActsByOsiId(int osiId);

        Task<bool> IsNeedSignNewOffer(int osiId);
        Task<Osi> GetOsiByIdWithoutIncludes(int id);
        Task AddInPromo(int osiId);
        Task<List<Abonent>> LoadAbonentsFromExcel(int osiId, byte[] data);

        Task RemakeAccuralsAtLastPlan(int osiId);
    }

    public class OsiSvc : IOsiSvc
    {
        #region Конструктор
        private readonly IServiceProvider serviceProvider;

        public OsiSvc(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
        #endregion

        #region Получение списков и моделей ОСИ
        public async Task<IEnumerable<Osi>> GetOsies()
        {
            using var db = OSIBillingDbContext.DbContext;
            var osies = await db.Osies.Include(o => o.HouseState).Include(o => o.UnionType).ToListAsync();
            return osies;
        }

        public async Task<IEnumerable<Osi>> GetActiveOsies()
        {
            using var db = OSIBillingDbContext.DbContext;
            var osies = await db.Osies.Where(o => o.IsLaunched).Include(o => o.HouseState).Include(o => o.UnionType).ToListAsync();
            return osies;
        }

        public async Task<Osi> GetOsiById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var osi = await db.Osies.Include(o => o.HouseState).Include(o => o.UnionType).FirstOrDefaultAsync(o => o.Id == id);
            if (osi == null)
                throw new Exception("Объект ОСИ не найден");
            return osi;
        }

        public async Task<Osi> GetOsiByIdWithoutIncludes(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var osi = await db.Osies.FirstOrDefaultAsync(o => o.Id == id);
            if (osi == null)
                throw new Exception("Объект ОСИ не найден");
            return osi;
        }

        public async Task CheckOsiById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (!await db.Osies.AnyAsync(o => o.Id == id))
                throw new Exception("Объект ОСИ не найден");
        }

        public async Task<IEnumerable<Osi>> GetActiveOsiByUserId(int userId)
        {
            using var db = OSIBillingDbContext.DbContext;
            var osies = await db.OsiUsers
                .Include(ou => ou.Osi).ThenInclude(o => o.HouseState)
                .Include(ou => ou.Osi).ThenInclude(o => o.UnionType)
                .Where(ou => ou.UserId == userId).Select(o => o.Osi).ToListAsync();
            return osies;
        }
        #endregion

        #region Изменение/удаление ОСИ
        public async Task UpdateOsi(int id, OsiRequest request, ClaimsPrincipal user)
        {
            //if (request.ConstructionYear > DateTime.Today.Year)
            //    throw new Exception($"Год постройки не может быть больше {DateTime.Today.Year}");

            using var db = OSIBillingDbContext.DbContext;
            Osi osi = await GetOsiByIdWithoutIncludes(id);
            if (osi.Idn != request.Idn && user?.IsInRole("ADMIN") != true)
            {
                throw new Exception("Изменение ИИН/БИН запрещено, обратитесь в службу поддержки");
            }
            osi = serviceProvider.GetRequiredService<IMapper>().Map(request, osi); // используем автомаппер для редактирования существующего объекта            
            db.Entry(osi).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        public async Task AddOrUpdateModel(Osi model)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (model.Id == default)
            {
                db.Osies.Add(model);
            }
            else
            {
                db.Osies.Update(model);
            }
            await db.SaveChangesAsync();
        }
        #endregion

        #region Старт/Стоп ОСИ
        public async Task StartOsi(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            Osi osi = await GetOsiByIdWithoutIncludes(id);
            osi.IsLaunched = true;
            db.Entry(osi).Property(o => o.IsLaunched).IsModified = true;
            await db.SaveChangesAsync();
        }

        public async Task StopOsi(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            Osi osi = await GetOsiByIdWithoutIncludes(id);
            osi.IsLaunched = false;
            db.Entry(osi).Property(o => o.IsLaunched).IsModified = true;
            await db.SaveChangesAsync();
        }

        public async Task ActivateOsi(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            Osi osi = await GetOsiByIdWithoutIncludes(id);
            osi.IsActive = true;
            db.Entry(osi).Property(o => o.IsActive).IsModified = true;
            await db.SaveChangesAsync();
        }

        public async Task DeactivateOsi(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            Osi osi = await GetOsiByIdWithoutIncludes(id);
            osi.IsActive = false;
            db.Entry(osi).Property(o => o.IsActive).IsModified = true;
            await db.SaveChangesAsync();
        }
        #endregion

        #region Документы ОСИ
        public async Task<OsiDoc> AddOsiDoc(int osiId, AddScanDoc request)
        {
            //DocType docType = await db.DocTypes.FirstOrDefaultAsync(d => d.Id == request.DocTypeId);
            //if (docType == null)
            //    throw new Exception("Неверный тип документа");

            Osi osi = await GetOsiByIdWithoutIncludes(osiId);

            string fileName = osi.Idn + "_" + request.DocTypeCode + "_" + DateTime.Now.Ticks.ToString() + "." + request.Extension.Replace(".", "");
            Scan scan = await serviceProvider.GetRequiredService<IScanSvc>().SaveDataToFile(fileName, request.Data);

            OsiDoc dbModel = new OsiDoc
            {
                DocTypeCode = request.DocTypeCode,
                OsiId = osiId,
                ScanId = scan.Id,
                CreateDt = DateTime.Today
            };
            using var db = OSIBillingDbContext.DbContext;
            db.OsiDocs.Add(dbModel);
            await db.SaveChangesAsync();

            dbModel = await db.OsiDocs
                .Include(o => o.DocType)
                .Include(o => o.Scan)
                .FirstAsync(o => o.Id == dbModel.Id);

            return dbModel;
        }

        public async Task<IEnumerable<OsiDoc>> GetOsiDocs(int osiId)
        {
            using var db = OSIBillingDbContext.DbContext;
            var osiDocs = await db.OsiDocs
                .Include(o => o.DocType)
                .Include(o => o.Scan)
                .Where(o => o.OsiId == osiId)
                .OrderBy(o => o.CreateDt)
                .ToListAsync();
            return osiDocs;
        }

        public async Task DeleteOsiDoc(int osiId, int docId)
        {
            using var db = OSIBillingDbContext.DbContext;
            OsiDoc doc = await db.OsiDocs.FirstOrDefaultAsync(o => o.Id == docId && o.OsiId == osiId);
            if (doc == null)
                throw new Exception("Документ не найден");

            db.OsiDocs.Remove(doc);

            // если на заявках нет такого скана, то удаляем из сканов
            if (!db.RegistrationDocs.Any(r => r.ScanId == doc.ScanId))
            {
                await serviceProvider.GetRequiredService<IScanSvc>().DeleteScanById(doc.ScanId);
            }

            await db.SaveChangesAsync();
        }
        #endregion

        public async Task<IEnumerable<Abonent>> GetAbonentsByOsiId(int osiId, bool onlyExternals = false, bool withDeactivated = false)
        {
            await CheckOsiById(osiId);
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.Abonents
                .Include(a => a.AreaType)
                .Where(a => a.OsiId == osiId)
                .Where(a => (!onlyExternals || a.External))
                .Where(a => (withDeactivated || a.IsActive))
                .ToListAsync();
            return models.OrderBy(a => a.Flat, NaturalComparer.Instance);
        }

        // OSI-169
        public async Task<Abonent> GetAbonentByOsiIdAndFlat(int osiId, string flat)
        {
            await CheckOsiById(osiId);
            using var db = OSIBillingDbContext.DbContext;
            var abonent = await db.Abonents
                .Include(a => a.AreaType)
                .Where(a => a.OsiId == osiId && a.Flat.ToUpper() == flat.ToUpper())
                .OrderBy(a => a.AreaTypeCode)
                .FirstOrDefaultAsync();
            if (abonent == null)
                throw new Exception("Абонент не найден");
            return abonent;
        }

        public async Task<IEnumerable<User>> GetOsiUsers(int osiId)
        {
            await CheckOsiById(osiId);
            using var db = OSIBillingDbContext.DbContext;
            var users = await db.Osies.Where(o => o.Id == osiId).SelectMany(o => o.OsiUsers.Select(ou => ou.User)).ToListAsync();
            return users;
        }

        public async Task SaveWizardStep(int osiId, string wizardStep)
        {
            var osi = await GetOsiByIdWithoutIncludes(osiId);
            osi.WizardStep = wizardStep;
            using var db = OSIBillingDbContext.DbContext;
            db.Entry(osi).Property(o => o.WizardStep).IsModified = true;
            db.SaveChanges();
        }

        public async Task<IEnumerable<Act>> GetActsByOsiId(int osiId, ActStateCodes state)
        {
            await CheckOsiById(osiId);
            using var db = OSIBillingDbContext.DbContext;
            var acts = await db.Acts.Include(a => a.State).Where(a => a.OsiId == osiId && a.StateCode == state).ToListAsync();
            return acts;
        }

        //public async Task<IEnumerable<Act>> GetSignedAndProvActsByOsiId(int osiId)
        //{
        //    await CheckOsiById(osiId);
        //    using var db = dbContext;
        //    var acts = await db.Acts.Include(a => a.State).Where(a => a.OsiId == osiId && (a.StateCode == ActStateCodes.SIGNED || a.StateCode == ActStateCodes.PROV)).ToListAsync();
        //    return acts;
        //}

        public async Task<bool> IsNeedSignNewOffer(int osiId)
        {
            using var db = OSIBillingDbContext.DbContext;
            var signedContract = await db.OsiDocs.OrderByDescending(o => o.CreateDt).FirstOrDefaultAsync(o => o.OsiId == osiId && o.DocTypeCode == "SIGNED_CONTRACT");
            return (signedContract?.CreateDt ?? new DateTime(1999, 1, 1)) < new DateTime(2023, 3, 31);
        }

        public async Task AddInPromo(int osiId)
        {
            using var db = OSIBillingDbContext.DbContext;
            var osi = await db.Osies.FirstOrDefaultAsync(o => o.Id == osiId);
            if (osi == null)
                throw new Exception("Объект ОСИ не найден");

            if (osi.IsInPromo)
            {
                throw new Exception("Данный ОСИ уже участник акции");
            }

            if (db.Acts.Any(a => a.OsiId == osiId))
            {
                throw new Exception("Данный ОСИ не может участвовать в акции, т.к. ему уже были выставлены акты");
            }

            osi.FreeMonthPromo = 1;
            osi.IsInPromo = true;
            db.Entry(osi).Property(a => a.FreeMonthPromo).IsModified = true;
            db.Entry(osi).Property(a => a.IsInPromo).IsModified = true;

            await db.SaveChangesAsync();
        }

        public async Task<List<Abonent>> LoadAbonentsFromExcel(int osiId, byte[] data)
        {
            var loader = new ExcelAbonentsLoader();
            var osi = await GetOsiById(osiId);
            var parseAction = loader.ReadExcelFile(data);
            if (!parseAction.Success)
                throw new Exception(parseAction.ErrorMessage);

            using var db = OSIBillingDbContext.DbContext;
            var serviceGroupSaldoSvc = serviceProvider.GetRequiredService<IServiceGroupSaldoSvc>();
            var loadAction = await loader.LoadAbonents(serviceGroupSaldoSvc, parseAction.Abonents, osi, db);
            if (!loadAction.IsSuccess)
                throw new Exception(loadAction.ErrorMessage);

            return loadAction.Abonents;
        }

        public async Task RemakeAccuralsAtLastPlan(int osiId)
        {
            using var db = OSIBillingDbContext.DbContext;
            _ = await GetOsiById(osiId);
            PlanAccural plan = await PlanAccuralLogic.GetLastPlanAccuralByOsiIdOrCreateNew(osiId);
            await serviceProvider.GetRequiredService<ITransactionSvc>().CreateAccuralsByPlanId(plan.Id, true);
        }
    }
}
