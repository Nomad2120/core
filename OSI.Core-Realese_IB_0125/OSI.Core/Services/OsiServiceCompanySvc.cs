using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IOsiServiceCompanySvc
    {
        //errors
        string GetExceptionMessage(Exception ex);

        //check
        Task CheckOsiServiceCompanyById(int id);

        //get
        Task<OsiServiceCompany> GetOsiServiceCompanyById(int id);
        Task<IEnumerable<OsiServiceCompany>> GetOsiServiceCompanysByOsiId(int osiId);

        //crud
        Task<OsiServiceCompany> AddOrUpdateOsiServiceCompany(int id, OsiServiceCompanyRequest request);
        Task DeleteOsiServiceCompany(int id);
    }

    public class OsiServiceCompanySvc : IOsiServiceCompanySvc
    {
        #region Конструктор
        private readonly IOsiSvc osiSvc;

        public OsiServiceCompanySvc(IOsiSvc osiSvc)
        {
            this.osiSvc = osiSvc;
        }
        #endregion

        public string GetExceptionMessage(Exception ex)
        {
            string message = ex.Message;
            if (ex is DbUpdateException)
            {
                if (ex.InnerException.Message.IndexOf("duplicate key") > -1)
                {
                    if (ex.InnerException.Message.IndexOf("unq_osi_service_companies_osi") > -1)
                        message = "Такая сервисная компания уже есть на данном ОСИ";
                }
            }
            return message;
        }

        public async Task CheckOsiServiceCompanyById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (!await db.OsiServiceCompanies.AnyAsync(o => o.Id == id))
                throw new Exception("Сервисная компания ОСИ не найдена");
        }

        public async Task<IEnumerable<OsiServiceCompany>> GetOsiServiceCompanysByOsiId(int osiId)
        {
            await osiSvc.CheckOsiById(osiId);
            using var db = OSIBillingDbContext.DbContext;
            var osiServiceCompanys = await db.OsiServiceCompanies
                .Include(s => s.ServiceCompany)
                .Where(s => s.OsiId == osiId).ToListAsync();
            return osiServiceCompanys;
        }

        public async Task<OsiServiceCompany> GetOsiServiceCompanyById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var osiServiceCompany = await db.OsiServiceCompanies
                .Include(s => s.ServiceCompany)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (osiServiceCompany == null)
                throw new Exception("Сервисная компания ОСИ не найдена");

            return osiServiceCompany;
        }

        public async Task<OsiServiceCompany> AddOrUpdateOsiServiceCompany(int id, OsiServiceCompanyRequest request)
        {
            await osiSvc.CheckOsiById(request.OsiId);
            OsiServiceCompany model = null;
            if (id == default) model = new OsiServiceCompany();
            else model = await GetOsiServiceCompanyById(id);

            model.ServiceCompany = null;
            model.Osi = null;
            model.OsiId = request.OsiId;
            model.ServiceCompanyCode = request.ServiceCompanyCode;
            model.Phones = request.Phones;
            model.Addresses = request.Addresses;
            model.ShowPhones = request.ShowPhones;

            using var db = OSIBillingDbContext.DbContext;
            if (id == default) db.OsiServiceCompanies.Add(model);
            else db.OsiServiceCompanies.Update(model);
            await db.SaveChangesAsync();

            // обновим модель для подгрузки связок
            if (id == default)
            {
                model = await GetOsiServiceCompanyById(model.Id);
                return model;
            }
            else return null;
        }

        public async Task DeleteOsiServiceCompany(int id)
        {
            OsiServiceCompany osiServiceCompany = await GetOsiServiceCompanyById(id);
            using var db = OSIBillingDbContext.DbContext;
            db.OsiServiceCompanies.Remove(osiServiceCompany);
            await db.SaveChangesAsync();
        }
    }
}
