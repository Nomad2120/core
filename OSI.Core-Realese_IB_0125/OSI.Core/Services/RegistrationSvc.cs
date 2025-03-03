using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OSI.Core.Models.Enums;
using OSI.Core.Logic;
using AutoMapper;
using Microsoft.Extensions.Logging;
using OSI.Core.Models.Responses;

namespace OSI.Core.Services
{
    public interface IRegistrationSvc
    {
        Task<Registration> GetRegistrationById(int id);
        Task<IEnumerable<Registration>> GetRegistrationsByUserId(int userId);
        Task<int> AddRegistration(RegistrationRequest request);
        Task UpdateRegistration(int id, RegistrationRequest request);
        Task<RegistrationDoc> AddRegistrationDoc(int registrationId, AddScanDoc request);
        Task<IEnumerable<RegistrationDoc>> GetRegistrationDocs(int registrationId);
        Task<IEnumerable<Registration>> GetRegistrations();
        Task<IEnumerable<RequiredDocsResponse>> GetRequirmentsDocs(int registrationId);
        Task DeleteRegistrationDoc(int registrationId, int docId);
        Task ConfirmRegistrationById(int id);
        Task ConfirmRegistration(Registration registration);
        Task<Osi> CreateOsiByRegistrationId(int registrationId);
        Task<Osi> CreateOsiByRegistration(Registration registration);
        Task DeleteRegistration(int id);
        Task SignRegistration(int id, string extension, byte[] data);
        Task UnsignRegistration(int id);
        Task AddOrUpdateModel(Registration model);
        Task<IEnumerable<RegistrationState>> GetRegistrationStates();
        Task<Registration> GetRegistrationByIdWithoutIncludes(int id);
        Task CheckRegistrationById(int registrationId);
        Task SaveWizardStep(int registrationId, string wizardStep);
        Task SignRegistrationWithoutDoc(int id);
        Task ConfirmCreation(int id);
        Task RejectRegistration(Registration registration, string reason);
    }

    public class RegistrationSvc : IRegistrationSvc
    {
        #region Конструктор
        private readonly IScanSvc scanSvc;
        private readonly IUserSvc userSvc;
        private readonly ITelegramBotSvc telegramBotSvc;
        private readonly ITariffSvc tariffSvc;
        private readonly IAccountReportSvc accountReportSvc;
        private readonly IOsiAccountSvc osiAccountSvc;
        private readonly IMapper mapper;
        private readonly ILogger<RegistrationSvc> logger;

        public RegistrationSvc(IScanSvc scanSvc,
                               IUserSvc userSvc,
                               ITelegramBotSvc telegramBotSvc,
                               ITariffSvc tariffSvc,
                               IAccountReportSvc accountReportSvc,
                               IOsiAccountSvc osiAccountSvc,
                               IMapper mapper,
                               ILogger<RegistrationSvc> logger)
        {
            this.scanSvc = scanSvc;
            this.userSvc = userSvc;
            this.telegramBotSvc = telegramBotSvc;
            this.tariffSvc = tariffSvc;
            this.accountReportSvc = accountReportSvc;
            this.osiAccountSvc = osiAccountSvc;
            this.mapper = mapper;
            this.logger = logger;
        }
        #endregion

        #region Работа с заявкой
        public async Task<IEnumerable<Registration>> GetRegistrations()
        {
            using var db = OSIBillingDbContext.DbContext;
            var registrations = await db.Registrations.Include(r => r.State).Include(o => o.UnionType).ToListAsync();
            return registrations;
        }

        public async Task<Registration> GetRegistrationById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var registration = await db.Registrations.Include(r => r.State).Include(o => o.UnionType).FirstOrDefaultAsync(r => r.Id == id);
            if (registration == null)
                throw new Exception("Заявка не найдена");
            return registration;
        }

        public async Task CheckRegistrationById(int registrationId)
        {
            using var db = OSIBillingDbContext.DbContext;
            _ = await db.Registrations.FirstOrDefaultAsync(r => r.Id == registrationId) ?? throw new Exception("Заявка не найдена");
        }

        public async Task<Registration> GetRegistrationByIdWithoutIncludes(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var registration = await db.Registrations.FirstOrDefaultAsync(r => r.Id == id);
            if (registration == null)
                throw new Exception("Заявка не найдена");
            return registration;
        }

        public async Task<IEnumerable<Registration>> GetRegistrationsByUserId(int userId)
        {
            _ = await userSvc.GetUserById(userId);
            using var db = OSIBillingDbContext.DbContext;
            var registrations = await db.Registrations.Include(r => r.State).Include(o => o.UnionType).Where(r => r.UserId == userId).ToListAsync();
            return registrations;
        }

        public async Task AddOrUpdateModel(Registration model)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (model.Id == default)
            {
                db.Registrations.Add(model);
            }
            else
            {
                db.Registrations.Update(model);
            }
            await db.SaveChangesAsync();
        }

        public async Task<int> AddRegistration(RegistrationRequest request)
        {
            _ = await userSvc.GetUserById(request.UserId);

            // Определяем тип заявки
            var registrationKind = "INITIAL";
            var reqTypeCode = "INITIAL_REGISTRATION";
            var registrationStateCode = RegistrationStateCodes.CREATED;

            using var db = OSIBillingDbContext.DbContext;
            var osi = await db.Osies.FirstOrDefaultAsync(o => o.Rca == request.Rca && o.IsLaunched);
            if (osi != null)
            {
                if (osi.UnionTypeId != request.UnionTypeId) // Если изменился тип объединения то это CHANGE_UNION_TYPE
                {
                    registrationKind = "CHANGE_UNION_TYPE";
                }
                else if (osi.Idn != request.Idn) // Если изменился БИН ОСИ то это тоже CHANGE_UNION_TYPE
                {
                    registrationKind = "CHANGE_UNION_TYPE";
                }
                else // Если БИН не изменился, то это смена председателя
                {
                    if (await db.OsiUsers.AnyAsync(ou => ou.OsiId == osi.Id && ou.UserId == request.UserId)) // Если это ОСИ того же пользователя то выдаем ошибку
                    {
                        throw new Exception("По данному адресу у Вас уже зарегистрировано ОСИ");
                    }
                    registrationKind = "CHANGE_CHAIRMAN";
                }
                reqTypeCode = "RE_REGISTRATION";
                registrationStateCode = RegistrationStateCodes.PREPARED;
            }

            var registration = mapper.Map<Registration>(request); // используем автомаппер для создания нового объекта

            registration.RegistrationType = request.RegistrationType.ToUpper();
            if (registration.RegistrationType != "FULL" && registration.RegistrationType != "FREE")
                throw new Exception("Тип заявки должен быть: FULL-платная, FREE-бесплатная");

            registration.RegistrationKind = registrationKind;
            registration.ReqTypeCode = reqTypeCode;
            registration.StateCode = registrationStateCode;
            registration.CreateDt = DateTime.Now;
            // OSI-122
            registration.Tariff = await tariffSvc.GetTariffValueByRca(registration.Rca);
            db.Registrations.Add(registration);
            await db.SaveChangesAsync();
            return registration.Id;
        }

        public async Task UpdateRegistration(int id, RegistrationRequest request)
        {
            _ = await userSvc.GetUserById(request.UserId);
            var registration = await GetRegistrationByIdWithoutIncludes(id);

            // Определяем тип заявки
            var registrationKind = "INITIAL";
            var reqTypeCode = "INITIAL_REGISTRATION";
            var registrationStateCode = RegistrationStateCodes.CREATED;

            using var db = OSIBillingDbContext.DbContext;
            var osi = await db.Osies.FirstOrDefaultAsync(o => o.Rca == request.Rca && o.IsLaunched);
            if (osi != null)
            {
                if (osi.UnionTypeId != request.UnionTypeId) // Если изменился тип объединения то это CHANGE_UNION_TYPE
                {
                    registrationKind = "CHANGE_UNION_TYPE";
                }
                else if (osi.Idn != request.Idn) // Если изменился БИН ОСИ то это тоже CHANGE_UNION_TYPE
                {
                    registrationKind = "CHANGE_UNION_TYPE";
                }
                else // Если БИН не изменился, то это смена председателя
                {
                    if (await db.OsiUsers.AnyAsync(ou => ou.OsiId == osi.Id && ou.UserId == request.UserId)) // Если это ОСИ того же пользователя то выдаем ошибку
                    {
                        throw new Exception("По данному адресу у Вас уже зарегистрировано ОСИ");
                    }
                    registrationKind = "CHANGE_CHAIRMAN";
                }
                reqTypeCode = "RE_REGISTRATION";
                registrationStateCode = RegistrationStateCodes.PREPARED;
            }

            var deleteAccounts = registration.RegistrationKind != registrationKind;
            var deleteDocs = registration.UnionTypeId != request.UnionTypeId || registration.ReqTypeCode != reqTypeCode;

            // Если сменился тип заявки, то меняем значения 
            if (registration.RegistrationKind != registrationKind)
            {
                registration.RegistrationKind = registrationKind;
                registration.ReqTypeCode = reqTypeCode;
                registration.StateCode = registrationStateCode;
                registration.WizardStep = null;
            }

            registration = mapper.Map(request, registration); // используем автомаппер для редактирования существующего объекта

            registration.RegistrationType = request.RegistrationType.ToUpper();
            if (registration.RegistrationType != "FULL" && registration.RegistrationType != "FREE")
                throw new Exception("Тип заявки должен быть: FULL-платная, FREE-бесплатная");

            // OSI-122
            if (registration.Rca != request.Rca)
                registration.Tariff = await tariffSvc.GetTariffValueByRca(request.Rca);

            db.Entry(registration).State = EntityState.Modified;
            await db.SaveChangesAsync();

            // Удаляем счета
            if (deleteAccounts)
            {
                var registrationAccounts = await db.RegistrationAccounts
                    .Where(s => s.RegistrationId == id)
                    .ToListAsync();
                db.RemoveRange(registrationAccounts);
                await db.SaveChangesAsync();
            }

            // Удаляем лишние документы
            if (deleteDocs)
            {
                var reqDocs = await GetRequirmentsDocs(registration.ReqTypeCode, registration.UnionTypeId);
                var docs = await GetRegistrationDocs(id);
                foreach (var doc in docs)
                {
                    if (!reqDocs.Any(rd => rd.Code == doc.DocTypeCode))
                    {
                        await DeleteRegistrationDoc(id, doc.Id);
                    }
                }
            }
        }

        public async Task ConfirmCreation(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var registration = await GetRegistrationByIdWithoutIncludes(id);
            if (registration.StateCode != RegistrationStateCodes.PREPARED)
            {
                var mustBeState = await db.RegistrationStates.FirstOrDefaultAsync(rs => rs.Code == RegistrationStateCodes.PREPARED);
                throw new Exception($"Заявка должна быть в состоянии '{mustBeState?.Name}'");
            }
            registration.StateCode = RegistrationStateCodes.CREATED;
            db.Entry(registration).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        public async Task SignRegistration(int id, string extension, byte[] data)
        {
            using var db = OSIBillingDbContext.DbContext;
            var registration = await GetRegistrationByIdWithoutIncludes(id);
            if (registration.StateCode != RegistrationStateCodes.CREATED)
            {
                var mustBeState = await db.RegistrationStates.FirstOrDefaultAsync(rs => rs.Code == RegistrationStateCodes.CREATED);
                throw new Exception($"Заявка должна быть в состоянии '{mustBeState?.Name}'");
            }
            registration.StateCode = RegistrationStateCodes.SIGNED;
            db.Entry(registration).State = EntityState.Modified;

            var signDoc = await AddRegistrationDoc(registration.Id, new AddScanDoc
            {
                DocTypeCode = "SIGNED_CONTRACT",
                Data = data,
                Extension = extension
            });
            await db.SaveChangesAsync();
            _ = telegramBotSvc.SendRegistrationSignedNotification(registration);
        }

        /// <summary>
        /// Подписать без документа
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task SignRegistrationWithoutDoc(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var registration = await GetRegistrationByIdWithoutIncludes(id);
            if (registration.StateCode != RegistrationStateCodes.CREATED)
            {
                var mustBeState = await db.RegistrationStates.FirstOrDefaultAsync(rs => rs.Code == RegistrationStateCodes.CREATED);
                throw new Exception($"Заявка должна быть в состоянии '{mustBeState?.Name}'");
            }
            registration.StateCode = RegistrationStateCodes.SIGNED;
            db.Entry(registration).State = EntityState.Modified;
            await db.SaveChangesAsync();
            _ = telegramBotSvc.SendRegistrationSignedNotification(registration);
        }

        /// <summary>
        /// Убрать подписание заявки
        /// </summary>
        /// <param name="id">Id заявки</param>
        /// <returns></returns>
        public async Task UnsignRegistration(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var registration = await GetRegistrationByIdWithoutIncludes(id);
            if (registration.StateCode != RegistrationStateCodes.SIGNED)
            {
                var mustBeState = await db.RegistrationStates.FirstOrDefaultAsync(rs => rs.Code == RegistrationStateCodes.SIGNED);
                throw new Exception($"Заявка должна быть в состоянии '{mustBeState?.Name}'");
            }
            registration.StateCode = RegistrationStateCodes.CREATED;
            db.Entry(registration).State = EntityState.Modified;

            // если уже есть подписанный договор, то удалим старый
            var regDocs = await GetRegistrationDocs(registration.Id);
            if (regDocs != null && regDocs.Any(rd => rd.DocTypeCode == "SIGNED_CONTRACT"))
            {
                var oldSignDoc = regDocs.FirstOrDefault(rd => rd.DocTypeCode == "SIGNED_CONTRACT");
                await DeleteRegistrationDoc(registration.Id, oldSignDoc.Id);
            }

            await db.SaveChangesAsync();
        }

        public async Task ConfirmRegistrationById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var registration = await GetRegistrationByIdWithoutIncludes(id);
            await ConfirmRegistration(registration);
        }

        public async Task ConfirmRegistration(Registration registration)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (registration.StateCode != RegistrationStateCodes.SIGNED)
            {
                var mustBeState = await db.RegistrationStates.FirstOrDefaultAsync(rs => rs.Code == RegistrationStateCodes.SIGNED);
                throw new Exception($"Заявка должна быть в состоянии '{mustBeState?.Name}'");
            }

            // OSI-604, смена председателя, перенос доков
            // OSI-620, смена формы правления
            if (registration.RegistrationKind == "CHANGE_CHAIRMAN" || registration.RegistrationKind == "CHANGE_UNION_TYPE")
            {
                // ищем оси
                var osi = await db.Osies.FirstOrDefaultAsync(o => o.Rca == registration.Rca);
                if (osi == null)
                    throw new Exception($"ОСИ с РКА '{registration.Rca}' не найден");

                osi.Fio = registration.Fio;
                osi.Phone = registration.Phone;
                db.Entry(osi).Property(o => o.Fio).IsModified = true;
                db.Entry(osi).Property(o => o.Phone).IsModified = true;

                // OSI-620, смена формы правления, дополнительные поля
                if (registration.RegistrationKind == "CHANGE_UNION_TYPE")
                {
                    osi.Idn = registration.Idn;
                    osi.UnionTypeId = registration.UnionTypeId;
                    osi.Name = registration.Name;
                    db.Entry(osi).Property(o => o.Idn).IsModified = true;
                    db.Entry(osi).Property(o => o.UnionTypeId).IsModified = true;
                    db.Entry(osi).Property(o => o.Name).IsModified = true;

                    var osiAccounts = await db.OsiAccounts.Where(a => a.OsiId == osi.Id).ToListAsync();
                    var kapRemont = await db.OsiServices.FirstOrDefaultAsync(a => a.OsiId == osi.Id && a.ServiceGroupId == 2);
                    var registrationAccounts = await db.RegistrationAccounts.Where(r => r.RegistrationId == registration.Id).ToListAsync();
                    var currentAccountOsi = osiAccounts?.FirstOrDefault(s => s.AccountTypeCode == AccountTypeCodes.CURRENT);
                    var savingAccountOsi = osiAccounts?.FirstOrDefault(s => s.AccountTypeCode == AccountTypeCodes.SAVINGS);
                    var currentAccountRegistration = registrationAccounts?.FirstOrDefault(s => s.AccountTypeCode == AccountTypeCodes.CURRENT);
                    var savingAccountRegistration = registrationAccounts?.FirstOrDefault(s => s.AccountTypeCode == AccountTypeCodes.SAVINGS);

                    // проверка текущего счета
                    OsiAccount newOsiAccountCurrent = null;
                    // а - если на оси-преемнике прописаны сбер счет и услуга, а на заявке заполнен сбер счет, то нужно прописать новый сбер счет с заявки;
                    if (currentAccountOsi != null)
                    {
                        if (currentAccountRegistration != null && (currentAccountOsi.Account != currentAccountRegistration.Account || currentAccountOsi.BankBic != currentAccountRegistration.BankBic))
                        {
                            newOsiAccountCurrent = new OsiAccount
                            {
                                Id = currentAccountOsi.Id,
                                Account = currentAccountRegistration.Account,
                                AccountTypeCode = AccountTypeCodes.CURRENT,
                                BankBic = currentAccountRegistration.BankBic,
                                OsiId = osi.Id
                            };
                        }
                    }
                    else
                    {
                        // если на оси приемнике нет текущего счета, а в заявке есть, то создать
                        if (currentAccountRegistration != null)
                        {
                            newOsiAccountCurrent = new OsiAccount
                            {
                                Account = currentAccountRegistration.Account,
                                AccountTypeCode = AccountTypeCodes.CURRENT,
                                BankBic = currentAccountRegistration.BankBic,
                                OsiId = osi.Id
                            };
                        }
                    }

                    // обязательный перенос текущего счета с заменой (обязательно через операцию, чтобы сработал триггер - при сдаче отчетов перед жильцами было верное формирование)
                    if (newOsiAccountCurrent != null)
                    {
                        _ = await osiAccountSvc.AddOrUpdateOsiAccount(newOsiAccountCurrent.Id, newOsiAccountCurrent);
                    }

                    // проверка сбер счета
                    OsiAccount newOsiAccountSaving = null;
                    // а - если на оси-преемнике прописаны сбер счет и услуга, а на заявке заполнен сбер счет, то нужно прописать новый сбер счет с заявки;
                    if (savingAccountOsi != null)
                    {
                        if (kapRemont != null)
                        {
                            if (savingAccountRegistration != null && (savingAccountOsi.Account != savingAccountRegistration.Account || savingAccountOsi.BankBic != savingAccountRegistration.BankBic))
                            {
                                newOsiAccountSaving = new OsiAccount
                                {
                                    Id = savingAccountOsi.Id,
                                    Account = savingAccountRegistration.Account,
                                    AccountTypeCode = AccountTypeCodes.SAVINGS,
                                    BankBic = savingAccountRegistration.BankBic,
                                    OsiId = osi.Id
                                };
                            }
                            // б - если на оси приемнике есть сбер счет, а в заявке не указан сбер счет, то деактивировать на оси услугу кап ремонт;
                            else
                            {
                                kapRemont.IsActive = false;
                                db.Entry(kapRemont).Property(o => o.IsActive).IsModified = true;
                            }
                        }
                    }
                    else
                    {
                        // в - если на оси приемнике нет сбер счета, а в заявке есть, то создать
                        if (savingAccountRegistration != null)
                        {
                            newOsiAccountSaving = new OsiAccount
                            {
                                Account = savingAccountRegistration.Account,
                                AccountTypeCode = AccountTypeCodes.SAVINGS,
                                BankBic = savingAccountRegistration.BankBic,
                                OsiId = osi.Id
                            };
                        }
                    }

                    if (newOsiAccountSaving != null)
                    {
                        _ = await osiAccountSvc.AddOrUpdateOsiAccount(newOsiAccountSaving.Id, newOsiAccountSaving);
                    }
                }

                // скопируем сканированные документы из заявки в ОСИ
                var registrationDocs = await db.RegistrationDocs
                    .Include(r => r.DocType)
                    .Include(r => r.Scan)
                    .Where(r => r.RegistrationId == registration.Id)
                    .ToListAsync();

                db.OsiDocs.AddRange(registrationDocs.Select(rd => new OsiDoc
                {
                    DocTypeCode = rd.DocTypeCode,
                    ScanId = rd.ScanId,
                    OsiId = osi.Id,
                    CreateDt = DateTime.Today
                }));

                // добавим пользователю роль председателя если ее нет
                var user = await userSvc.GetUserById(registration.UserId);
                var chairmanRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == "CHAIRMAN") ?? throw new Exception("Не найдена роль председателя");
                if (await db.UserRoles.FirstOrDefaultAsync(ur => ur.RoleId == chairmanRole.Id && ur.UserId == user.Id) == null)
                {
                    db.UserRoles.Add(new UserRole
                    {
                        RoleId = chairmanRole.Id,
                        UserId = user.Id
                    });
                }

                // изменим доступ пользователя оси
                var osiUsers = await db.OsiUsers.Where(r => r.OsiId == osi.Id).ToListAsync();
                foreach (var ou in osiUsers)
                {
                    ou.UserId = user.Id;
                    ou.CreateDt = DateTime.Now;
                    db.Entry(ou).State = EntityState.Modified;
                }

                // добавляем в историю заявок
                db.RegistrationHistories.Add(new RegistrationHistory()
                {
                    OsiId = osi.Id,
                    RegistrationId = registration.Id,
                    ApproveDate = DateTime.Now
                });
            }

            registration.StateCode = RegistrationStateCodes.CONFIRMED;
            registration.SignDt = DateTime.Now;
            db.Entry(registration).Property(r => r.StateCode).IsModified = true;
            db.Entry(registration).Property(r => r.SignDt).IsModified = true;
            await db.SaveChangesAsync();
        }

        public async Task RejectRegistration(Registration registration, string reason)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (registration.StateCode is RegistrationStateCodes.PREPARED or RegistrationStateCodes.CONFIRMED or RegistrationStateCodes.REJECTED or RegistrationStateCodes.CLOSED)
            {
                var mustNotBeState = await db.RegistrationStates.FirstOrDefaultAsync(rs => rs.Code == registration.StateCode);
                throw new Exception($"Заявка не должна быть в состоянии '{mustNotBeState?.Name}'");
            }

            registration.StateCode = RegistrationStateCodes.REJECTED;
            registration.RejectReason = reason;
            db.Entry(registration).State = EntityState.Modified;
            await db.SaveChangesAsync();
            _ = telegramBotSvc.SendRegistrationRejectedNotification(registration);
        }

        public async Task DeleteRegistration(int id)
        {
            var registration = await GetRegistrationByIdWithoutIncludes(id);
            var registraionDocs = (await GetRegistrationDocs(id)).ToList();
            using var db = OSIBillingDbContext.DbContext;
            if ((registraionDocs?.Count ?? 0) > 0)
            {
                db.RegistrationDocs.RemoveRange(registraionDocs);
            }
            db.Registrations.Remove(registration);
            await db.SaveChangesAsync();
        }
        #endregion

        #region Добавление ОСИ
        public async Task<Osi> CreateOsiByRegistrationId(int id)
        {
            Registration registration = await GetRegistrationByIdWithoutIncludes(id);
            return await CreateOsiByRegistration(registration);
        }

        public async Task<Osi> CreateOsiByRegistration(Registration registration)
        {
            using var db = OSIBillingDbContext.DbContext;
            if (registration.StateCode != RegistrationStateCodes.CONFIRMED)
            {
                var mustBeState = await db.RegistrationStates.FirstOrDefaultAsync(rs => rs.Code == RegistrationStateCodes.CONFIRMED);
                throw new Exception($"Заявка должна быть в состоянии '{mustBeState?.Name}'");
            }
            Osi osi = await db.Osies.FirstOrDefaultAsync(o => o.RegistrationId == registration.Id);
            if (osi != null)
                throw new Exception($"По данной заявке уже создан ОСИ, Id={osi.Id}");

            // находим пользователя
            var user = await userSvc.GetUserById(registration.UserId);

            // OSI-305 получаем КБе по типу объединения
            var kbe = await db.UnionTypes.Where(ut => ut.Id == registration.UnionTypeId).Select(ut => ut.Kbe).FirstOrDefaultAsync()
                ?? throw new Exception($"Не удается получить КБе по типу объединения");

            var chairmanRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == "CHAIRMAN") ?? throw new Exception("Не найдена роль председателя");

            using var dbTransaction = await db.Database.BeginTransactionAsync();
            // создаем ОСИ
            try
            {
                osi = new Osi
                {
                    AccuralsWithDecimals = false,
                    Address = registration.Address,
                    ApartCount = registration.ApartCount,
                    BigRepairMrpPercent = 0.005m,
                    CoefUnlivingArea = 100,
                    Email = registration.Email,
                    Fio = registration.Fio,
                    FreeMonthPromo = 0,
                    HouseStateCode = HouseStateCodes.NORMAL,
                    Idn = registration.Idn,
                    IsActive = true,
                    IsInPromo = false,
                    IsLaunched = false,
                    Kbe = kbe,
                    Name = registration.Name,
                    Phone = registration.Phone,
                    Rca = registration.Rca,
                    RegistrationId = registration.Id,
                    RegistrationType = registration.RegistrationType,
                    TakeComission = false, // OSI-342
                    UnionTypeId = registration.UnionTypeId,
                    CanRemakeAccurals = false
                };
                if (registration.RegistrationType.ToUpper() == "FREE")
                    osi.WizardStep = "finish";

                db.Osies.Add(osi);
                await db.SaveChangesAsync(); // сначала сохраним сам ОСИ

                // добавим доступ
                db.OsiUsers.Add(new OsiUser
                {
                    OsiId = osi.Id,
                    UserId = user.Id
                });

                // скопируем сканированные документы из заявки в ОСИ
                var registrationDocs = await db.RegistrationDocs
                        .Include(r => r.DocType)
                        .Include(r => r.Scan)
                        .Where(r => r.RegistrationId == registration.Id)
                        .ToListAsync();

                db.OsiDocs.AddRange(registrationDocs.Select(rd => new OsiDoc
                {
                    DocTypeCode = rd.DocTypeCode,
                    ScanId = rd.ScanId,
                    OsiId = osi.Id,
                    CreateDt = DateTime.Today
                }));

                // добавим абонентов по кол-ву квартир
                for (int i = 1; i <= registration.ApartCount; i++)
                {
                    db.Abonents.Add(new Abonent
                    {
                        AreaTypeCode = AreaTypeCodes.RESIDENTIAL,
                        Flat = i.ToString(),
                        Floor = 0,
                        Idn = "",
                        Name = "",
                        Phone = "",
                        Square = 0,
                        OsiId = osi.Id,
                        IsActive = true
                    });
                }
                await db.SaveChangesAsync(); // для использования абонентов в OsiBillingServiceLogic.CreateOsiBillingService

                // добавим услугу ОСИ биллинг и не активируем ее, т.к.по-умолчанию галочка включения услуги в тариф стоит в planAccuralSvc.CopyLastPlanOrCreateNew - обратная логика 
                var osiBillingService = await OsiBillingServiceLogic.CreateOsiBillingService(db, osi.Id, registration.Tariff);

                // добавим пользователю роль председателя если ее нет
                if (await db.UserRoles.FirstOrDefaultAsync(ur => ur.RoleId == chairmanRole.Id && ur.UserId == user.Id) == null)
                {
                    db.UserRoles.Add(new UserRole
                    {
                        RoleId = chairmanRole.Id,
                        UserId = user.Id
                    });
                }

                // добавляем значение тарифа на текущую дату
                db.OsiTariffs.Add(new OsiTariff
                {
                    Dt = DateTime.Now.Date,
                    OsiId = osi.Id,
                    Value = registration.Tariff
                });

                // добавление счетов
                var regAccs = await db.RegistrationAccounts.Where(a => a.RegistrationId == registration.Id).ToListAsync();
                db.OsiAccounts.AddRange(regAccs.Select(r => new OsiAccount
                {
                    Account = r.Account,
                    AccountTypeCode = r.AccountTypeCode,
                    BankBic = r.BankBic,
                    OsiId = osi.Id,
                    ServiceGroupId = r.ServiceGroupId
                }));

                await db.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                if (osi.RegistrationType == "FREE")
                {
                    try
                    {
                        await accountReportSvc.CreateAccountReport(new() { OsiId = osi.Id, Period = DateTime.Today.AddMonths(-1) });
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Ошибка создания отчета председателя перед жильцами на бесплатной ОСИ");
                    }
                }

                _ = telegramBotSvc.SendOsiCreatedNotification(registration);
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
            return osi;
        }
        #endregion

        #region Сканированные документы
        public async Task<RegistrationDoc> AddRegistrationDoc(int registrationId, AddScanDoc request)
        {
            using var db = OSIBillingDbContext.DbContext;
            Registration reg = await GetRegistrationByIdWithoutIncludes(registrationId);

            string fileName = reg.Idn + "_" + request.DocTypeCode + "_" + DateTime.Now.Ticks.ToString() + "." + request.Extension.Replace(".", "");
            Scan scan = await scanSvc.SaveDataToFile(fileName, request.Data);

            RegistrationDoc doc = new RegistrationDoc
            {
                DocTypeCode = request.DocTypeCode,
                RegistrationId = registrationId,
                ScanId = scan.Id
            };
            db.RegistrationDocs.Add(doc);
            await db.SaveChangesAsync();

            doc.Scan = scan;
            doc.DocType = await db.DocTypes.FirstOrDefaultAsync(d => d.Code == doc.DocTypeCode);
            return doc;
        }

        public async Task<IEnumerable<RegistrationDoc>> GetRegistrationDocs(int registrationId)
        {
            using var db = OSIBillingDbContext.DbContext;
            var registrationDocs = await db.RegistrationDocs
                .Include(r => r.DocType)
                .Include(r => r.Scan)
                .Where(r => r.RegistrationId == registrationId)
                .ToListAsync();
            return registrationDocs;
        }

        public async Task DeleteRegistrationDoc(int registrationId, int docId)
        {
            using var db = OSIBillingDbContext.DbContext;
            RegistrationDoc doc = await db.RegistrationDocs.FirstOrDefaultAsync(rd => rd.Id == docId && rd.RegistrationId == registrationId);
            if (doc == null)
                throw new Exception("Документ не найден");

            db.RegistrationDocs.Remove(doc);

            // если на ОСИ нет такого скана, то удаляем из сканов
            if (!db.OsiDocs.Any(r => r.ScanId == doc.ScanId))
            {
                await scanSvc.DeleteScanById(doc.ScanId);
            }

            await db.SaveChangesAsync();
        }
        #endregion

        #region Прочее
        public async Task<IEnumerable<RequiredDocsResponse>> GetRequirmentsDocs(int registrationId)
        {
            var registration = await GetRegistrationByIdWithoutIncludes(registrationId);
            return await GetRequirmentsDocs(registration.ReqTypeCode, registration.UnionTypeId);
        }

        private async Task<IEnumerable<RequiredDocsResponse>> GetRequirmentsDocs(string reqTypeCode, int unionTypeId)
        {
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.ReqDocs
                        .Include(r => r.DocType)
                        .Where(r => r.ReqTypeCode == reqTypeCode && r.UnionTypeId == unionTypeId)
                        .Select(r => new RequiredDocsResponse
                        {
                            Code = r.DocType.Code,
                            NameKz = r.DocType.NameKz,
                            NameRu = r.DocType.NameRu,
                            MaxSize = r.DocType.MaxSize,
                            IsRequired = r.IsRequired
                        }).ToListAsync();
            return models;
        }

        public async Task<IEnumerable<RegistrationState>> GetRegistrationStates()
        {
            using var db = OSIBillingDbContext.DbContext;
            var models = await db.RegistrationStates.ToListAsync();
            return models;
        }

        public async Task SaveWizardStep(int registrationId, string wizardStep)
        {
            var reg = await GetRegistrationByIdWithoutIncludes(registrationId);
            reg.WizardStep = wizardStep;
            using var db = OSIBillingDbContext.DbContext;
            db.Entry(reg).Property(o => o.WizardStep).IsModified = true;
            db.SaveChanges();
        }
        #endregion


    }
}
