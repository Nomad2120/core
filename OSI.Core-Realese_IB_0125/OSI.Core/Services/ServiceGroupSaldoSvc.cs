using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using OSI.Core.Models.Requests;
using OSI.Core.Models.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IServiceGroupSaldoSvc
    {
        Task<ServiceGroupSaldo> AddServiceGroupSaldoByRequest(ServiceGroupSaldoRequest request);
        Task CheckServiceGroupById(OSIBillingDbContext db, int id);
        Task DeleteServiceGroupSaldo(int id);
        string GetExceptionMessage(Exception ex);
        Task<ServiceGroup> GetServiceGroupById(int id);
        Task<IEnumerable<ServiceGroupSaldo>> GetServiceGroupSaldoByGroupId(int osiId, int groupId);
        Task<ServiceGroupSaldo> GetServiceGroupSaldoById(int id);
        Task LoadBeginSaldoFromFile(ServiceGroup group, Osi osi, bool replaceSaldo, string filename);
        Task UpdateServiceGroupSaldoByRequest(int id, ServiceGroupSaldoRequest request);
        Task<IEnumerable<ServiceGroupSaldoResponse>> GetServiceGroupSaldoByOsiId(int osiId);
        Task UpdateServiceGroupSaldoAmountById(int id, decimal saldo);
    }

    public class ServiceGroupSaldoSvc : IServiceGroupSaldoSvc
    {
        #region Конструктор
        private readonly IWebHostEnvironment env;
        private readonly ITransactionSvc transactionSvc;

        public ServiceGroupSaldoSvc(IWebHostEnvironment env, ITransactionSvc transactionSvc)
        {
            this.env = env;
            this.transactionSvc = transactionSvc;
        }
        #endregion

        public string GetExceptionMessage(Exception ex)
        {
            string message = ex.Message;
            if (ex is DbUpdateException)
            {
                if (ex.InnerException.Message.IndexOf("duplicate key") > -1)
                {
                    if (ex.InnerException.Message.IndexOf("unq_service_group_saldo_abonents") > -1)
                        message = "Сальдо по данному абоненту уже есть на данной услуге";
                }
            }
            return message;
        }

        public async Task CheckServiceGroupById(OSIBillingDbContext db, int id)
        {
            if (!await db.ServiceGroups.AnyAsync(o => o.Id == id))
                throw new Exception("Услуга не найдена");
        }

        public async Task<ServiceGroup> GetServiceGroupById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var serviceGroup = await db.ServiceGroups.FirstOrDefaultAsync(s => s.Id == id);
            if (serviceGroup == null)
                throw new Exception("Услуга не найдена");

            return serviceGroup;
        }

        public async Task<IEnumerable<ServiceGroupSaldo>> GetServiceGroupSaldoByGroupId(int osiId, int groupId)
        {
            using var db = OSIBillingDbContext.DbContext;
            await CheckServiceGroupById(db, groupId);
            var serviceGroupSaldos = await db.ServiceGroupSaldos
                .Include(s => s.Group)
                .Include(s => s.Abonent)
                .Include(s => s.Transaction)
                .Where(s => s.GroupId == groupId && s.OsiId == osiId)
                .ToListAsync();
            return serviceGroupSaldos.OrderBy(s => s.AbonentFlat.PadLeft(4, '0'));
        }

        public async Task<IEnumerable<ServiceGroupSaldoResponse>> GetServiceGroupSaldoByOsiId(int osiId)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (!(await db.Osies.AnyAsync(x => x.Id == osiId)))
                throw new Exception("ОСИ не найден");

            List<ServiceGroupSaldoResponse> result = new List<ServiceGroupSaldoResponse>();

            // только те группы, услуги которых есть у оси
            //var groups = await db.OsiServices
            //    .Include(x => x.Service)
            //    .ThenInclude(x => x.Group)
            //    .Where(x => x.OsiId == osiId)
            //    .GroupBy(x => new { Id = x.Service.Group.Id, NameRu = x.Service.Group.NameRu })
            //    .Select(x => new { Id = x.Key.Id, NameRu = x.Key.NameRu })
            //    .ToListAsync();
            var groups = await db.OsiServices
               .Include(x => x.ServiceGroup)
               .Where(x => x.OsiId == osiId && x.IsActive)
               .GroupBy(x => new { x.ServiceGroup.Id, x.ServiceGroup.NameRu, x.ServiceGroup.NameKz })
               .Select(x => new { x.Key.Id, x.Key.NameRu, x.Key.NameKz })
               .ToListAsync();

            var osiAbonents = await db.Abonents.Where(a => a.OsiId == osiId && a.IsActive).ToListAsync();

            foreach (var group in groups)
            {
                var items = new List<ServiceGroupSaldoResponseItem>();
                ServiceGroupSaldoResponse serviceGroupSaldoResponse = new ServiceGroupSaldoResponse
                {
                    GroupId = group.Id,
                    GroupNameRu = group.NameRu,
                    GroupNameKz = group.NameKz
                };

                var existingSaldos = await db.ServiceGroupSaldos
                    .Include(x => x.Abonent)
                    .Where(a => a.GroupId == group.Id)
                    .Where(a => a.OsiId == osiId)
                    .Where(a => a.Abonent.IsActive)
                    .ToListAsync();

                // чтобы потом сделать сверку по недостающим
                if (existingSaldos == null)
                    existingSaldos = new List<ServiceGroupSaldo>();

                if (existingSaldos.Any())
                {
                    items = existingSaldos.Select(x => new ServiceGroupSaldoResponseItem
                    {
                        Id = x.Id,
                        AbonentName = x.AbonentName,
                        AreaTypeCode = x.Abonent.AreaTypeCode,
                        Flat = x.AbonentFlat,
                        Saldo = x.Saldo
                    }).ToList();
                }

                // вычисляем недостающих
                var missingSaldos = osiAbonents.Where(x => (group.Id != 7 && !x.External) || (group.Id == 7 && x.External))
                    .Select(x => x.Id)?.Except(existingSaldos.Select(x => x.AbonentId).ToList());
                if (missingSaldos?.Any() ?? false)
                {
                    foreach (int abonentId in missingSaldos)
                    {
                        ServiceGroupSaldo model = new ServiceGroupSaldo
                        {
                            Saldo = 0,
                            GroupId = group.Id,
                            AbonentId = abonentId,
                            OsiId = osiId,
                            Transaction = new Transaction
                            {
                                AbonentId = abonentId,
                                Dt = new DateTime(1, 1, 1),
                                Amount = 0,
                                OsiId = osiId,
                                GroupId = group.Id,
                                TransactionType = TransactionTypeCodes.SALDO
                            }
                        };
                        db.ServiceGroupSaldos.Add(model);
                        await db.SaveChangesAsync();

                        Abonent abonent = osiAbonents.First(x => x.Id == abonentId);
                        items.Add(new ServiceGroupSaldoResponseItem
                        {
                            Id = model.Id,
                            AbonentName = abonent.Name,
                            AreaTypeCode = abonent.AreaTypeCode,
                            Flat = abonent.Flat,
                            Saldo = 0
                        });
                    }
                }
                serviceGroupSaldoResponse.Items = items.OrderBy(s => s.Flat?.PadLeft(4, '0'));
                result.Add(serviceGroupSaldoResponse);
            }
            return result;
        }

        public async Task<ServiceGroupSaldo> GetServiceGroupSaldoById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var serviceGroupSaldo = await db.ServiceGroupSaldos
                .Include(s => s.Group)
                .Include(s => s.Abonent)
                .Include(s => s.Transaction)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (serviceGroupSaldo == null) throw new Exception("Сальдо по услуге с таким ID не найдено");
            return serviceGroupSaldo;
        }

        private async Task ErrorIfAnyFineFound(OSIBillingDbContext db, int osiId)
        {
            bool result = await db.Transactions.AnyAsync(a => a.OsiId == osiId && a.TransactionType == TransactionTypeCodes.FINE);
            if (result)
            {
                throw new Exception("Уже проводилось начисление пени, поэтому изменение начального сальдо невозможно.");
            }
        }

        public async Task<ServiceGroupSaldo> AddServiceGroupSaldoByRequest(ServiceGroupSaldoRequest request)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (!(await db.Osies.AnyAsync(x => x.Id == request.OsiId)))
                throw new Exception("ОСИ не найден");
            
            if (!(await db.Abonents.AnyAsync(x => x.Id == request.AbonentId)))
                throw new Exception("Абонент не найден");
            
            await CheckServiceGroupById(db, request.GroupId);

            await ErrorIfAnyFineFound(db, request.OsiId);            

            ServiceGroupSaldo model = new ServiceGroupSaldo
            {
                Saldo = request.Saldo,
                GroupId = request.GroupId,
                AbonentId = request.AbonentId,
                OsiId = request.OsiId,
                Transaction = new Transaction
                {
                    AbonentId = request.AbonentId,
                    Dt = new DateTime(1, 1, 1),
                    Amount = request.Saldo,
                    OsiId = request.OsiId,
                    GroupId = request.GroupId,
                    TransactionType = TransactionTypeCodes.SALDO
                }
            };
            db.ServiceGroupSaldos.Add(model);
            await db.SaveChangesAsync();
            model = await GetServiceGroupSaldoById(model.Id); // для подгрузки связей 
            return model;
        }

        public async Task UpdateServiceGroupSaldoByRequest(int id, ServiceGroupSaldoRequest request)
        {
            ServiceGroupSaldo model = await GetServiceGroupSaldoById(id);
            using var db = OSIBillingDbContext.DbContext;
            
            if (!(await db.Osies.AnyAsync(x => x.Id == request.OsiId)))
                throw new Exception("ОСИ не найден");
            
            if (!(await db.Abonents.AnyAsync(x => x.Id == request.AbonentId)))
                throw new Exception("Абонент не найден");

            await CheckServiceGroupById(db, request.GroupId);

            await ErrorIfAnyFineFound(db, request.OsiId);

            // обновляем модель и проводку
            model.Abonent = null;
            model.Group = null;
            model.Osi = null;
            model.Saldo = request.Saldo;
            model.AbonentId = request.AbonentId;
            model.GroupId = request.GroupId;
            model.OsiId = request.OsiId;
            model.Transaction.Dt = new DateTime(1, 1, 1);
            model.Transaction.AbonentId = model.AbonentId;
            model.Transaction.Amount = model.Saldo;
            model.Transaction.GroupId = model.GroupId;
            model.Transaction.OsiId = model.OsiId;
            db.ServiceGroupSaldos.Update(model);
            await db.SaveChangesAsync();
        }

        public async Task UpdateServiceGroupSaldoAmountById(int id, decimal saldo)
        {
            ServiceGroupSaldo model = await GetServiceGroupSaldoById(id);
            model.Saldo = saldo;
            model.Transaction.Amount = model.Saldo;
            
            using var db = OSIBillingDbContext.DbContext;
            
            await ErrorIfAnyFineFound(db, model.OsiId);
            
            db.ServiceGroupSaldos.Update(model);
            await db.SaveChangesAsync();
        }

        public async Task DeleteServiceGroupSaldo(int id)
        {
            ServiceGroupSaldo model = await GetServiceGroupSaldoById(id);
            using var db = OSIBillingDbContext.DbContext;
            
            await ErrorIfAnyFineFound(db, model.OsiId);

            db.ServiceGroupSaldos.Remove(model);
            await db.SaveChangesAsync();
            await transactionSvc.DeleteTransactionById(model.TransactionId);
        }

        public async Task LoadBeginSaldoFromFile(ServiceGroup group, Osi osi, bool replaceSaldo, string filename)
        {
            string path = Path.Combine(env.WebRootPath, "load_saldo_files", filename);
            string[] lines = await File.ReadAllLinesAsync(path);
            if (!lines.Any())
                throw new Exception("Файл пуст");

            using var db = OSIBillingDbContext.DbContext;
            var abonents = await db.Abonents.Where(a => a.OsiId == osi.Id).ToListAsync();
            if (!abonents?.Any() ?? true)
                throw new Exception("Абоненты на ОСИ отсутствуют");

            List<ServiceGroupSaldo> oldSaldos = await db.ServiceGroupSaldos.Where(s => s.OsiId == osi.Id && s.GroupId == group.Id).ToListAsync();
            foreach (string line in lines.Skip(1))
            {
                string[] fields = line.Split('\t');

                // 0 - квартира
                string kv = fields[0].Trim();
                if (string.IsNullOrEmpty(kv))
                    throw new Exception($"Квартира не указана - {line}");

                Abonent abonent = abonents.FirstOrDefault(a => a.Flat == kv);
                if (abonent == null)
                    throw new Exception($"Квартира {kv} не найдена");

                // 1 - сумма
                string amountString = fields[1].Trim();
                if (!Decimal.TryParse(amountString, out decimal saldo))
                    throw new Exception($"Сумма {amountString} указана неверно");

                ServiceGroupSaldo serviceGroupSaldo = oldSaldos.FirstOrDefault(s => s.AbonentId == abonent.Id);
                if (serviceGroupSaldo == null)
                {
                    serviceGroupSaldo = await AddServiceGroupSaldoByRequest(new ServiceGroupSaldoRequest
                    {
                        AbonentId = abonent.Id,
                        GroupId = group.Id,
                        OsiId = osi.Id,
                        Saldo = saldo
                    });
                }
                else if (replaceSaldo)
                {
                    await UpdateServiceGroupSaldoAmountById(serviceGroupSaldo.Id, saldo);
                }
            }
        }
    }
}
