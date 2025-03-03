using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using OSI.Core.Models.Db;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OSI.Core.Pages;
using OSI.Core.Models.Enums;
using OSI.Core.Logic;
using System.Data.Common;
using System.Security.Cryptography;

namespace OSI.Core.Services
{
    using F = FieldNames;

    //enum FieldNames
    //{
    //    OsiId,
    //    AbonentId,
    //    ServiceCode,
    //    GroupId,
    //    ServiceName,
    //    Method,
    //    Tarif,
    //    Rezerv
    //}

    enum FieldNames
    {
        OsiId,
        Fio,
        Flat,
        Square,
        Resident,
        Iin,
        Phone,
        Saldo
    }

    public class TempClass
    {
        public int OsiId { get; set; }

        public string Fio { get; set; }

        public decimal Square { get; set; }

        public string Flat { get; set; }

        public AreaTypeCodes AreaTypeCode { get; set; }

        public string Iin { get; set; }

        public string Phone { get; set; }

        public decimal Saldo{ get; set; }
    }

    public static class TempSvc
    {
        //public static async Task ProcessServiceGroups()
        //{
        //    using var db = OSIBillingDbContext.DbContext;
        //    var sg1 = await db.ServiceGroups.FirstOrDefaultAsync(g => g.Id == 1); // Взнос на содержание общего имущества
        //    sg1.Code = "TO";
        //    db.ServiceGroups.Update(sg1);

        //    var sg2 = await db.ServiceGroups.FirstOrDefaultAsync(g => g.Id == 2); // Взнос на капитальный ремонт здания
        //    sg2.Code = "BIG_REPAIR";
        //    sg2.CanChangeName = false;
        //    sg2.JustOne = true;
        //    sg2.CanEditAbonents = false;
        //    db.ServiceGroups.Update(sg2);

        //    var sg3 = await db.ServiceGroups.FirstOrDefaultAsync(g => g.Id == 3); // Разовый целевой взнос
        //    sg3.Code = "TARGET_PAY";
        //    sg3.CopyToNextPeriod = false;
        //    db.ServiceGroups.Update(sg3);

        //    var sg4 = await db.ServiceGroups.FirstOrDefaultAsync(g => g.Id == 4); // Услуги паркинга
        //    sg4.Code = "PARKING";
        //    db.ServiceGroups.Update(sg4);

        //    // 5
        //    db.ServiceGroups.Add(new ServiceGroup
        //    {
        //        Code = "LIFT",
        //        NameRu = "Обслуживание лифта",
        //        NameKz = "Обслуживание лифта",
        //        AccountTypeCode = AccountTypeCodes.CURRENT,
        //        CanChangeName = true,
        //        JustOne = false,
        //        CopyToNextPeriod = true,
        //        CanEditAbonents = true
        //    });

        //    // 6
        //    db.ServiceGroups.Add(new ServiceGroup
        //    {
        //        Code = "BOILER",
        //        NameRu = "Услуги котельной",
        //        NameKz = "Услуги котельной",
        //        AccountTypeCode = AccountTypeCodes.CURRENT,
        //        CanChangeName = true,
        //        JustOne = false,
        //        CopyToNextPeriod = true,
        //        CanEditAbonents = true
        //    });
        //    await db.SaveChangesAsync();
        //}

        //public static async Task ProcessServiceNameExamples()
        //{
        //    using var db = OSIBillingDbContext.DbContext;

        //    db.ServiceNameExamples.Add(new ServiceNameExample
        //    {
        //        NameRu = "Техническое обслуживание",
        //        NameKz = "Техникалық қызмет көрсету",
        //        ServiceGroupId = 1
        //    });

        //    db.ServiceNameExamples.Add(new ServiceNameExample
        //    {
        //        NameRu = "Капитальный ремонт",
        //        NameKz = "Күрделі жөндеу",
        //        ServiceGroupId = 2
        //    });

        //    db.ServiceNameExamples.Add(new ServiceNameExample
        //    {
        //        NameRu = "Разовый целевой взнос",
        //        NameKz = "Бір реттік мақсатты жарна",
        //        ServiceGroupId = 3
        //    });

        //    db.ServiceNameExamples.Add(new ServiceNameExample
        //    {
        //        NameRu = "Паркинг",
        //        NameKz = "Көлік тұрағы",
        //        ServiceGroupId = 4
        //    });

        //    db.ServiceNameExamples.Add(new ServiceNameExample
        //    {
        //        NameRu = "Лифт",
        //        NameKz = "Жеделсаты",
        //        ServiceGroupId = 5
        //    });

        //    db.ServiceNameExamples.Add(new ServiceNameExample
        //    {
        //        NameRu = "Котельная",
        //        NameKz = "Қазандық",
        //        ServiceGroupId = 6
        //    });

        //    await db.SaveChangesAsync();
        //}

        //public static async Task ProcessAccuralMethods()
        //{
        //    using var db = OSIBillingDbContext.DbContext;

        //    db.AccuralMethods.Add(new AccuralMethod
        //    {
        //        Code = "TARIF_1KVM",
        //        Description = "Тариф за 1 кв.м.",
        //        AllowedAccuralMethods = new List<AllowedAccuralMethod>
        //        {
        //            new AllowedAccuralMethod { ServiceGroupId = 1 },
        //            new AllowedAccuralMethod { ServiceGroupId = 2 },
        //            new AllowedAccuralMethod { ServiceGroupId = 3 },
        //            new AllowedAccuralMethod { ServiceGroupId = 6 },
        //        }
        //    });

        //    db.AccuralMethods.Add(new AccuralMethod
        //    {
        //        Code = "FIX_SUM_FLAT",
        //        Description = "Фиксированная сумма с помещения",
        //        AllowedAccuralMethods = new List<AllowedAccuralMethod>
        //        {
        //            new AllowedAccuralMethod { ServiceGroupId = 1 },
        //            new AllowedAccuralMethod { ServiceGroupId = 3 },
        //            new AllowedAccuralMethod { ServiceGroupId = 4 },
        //            new AllowedAccuralMethod { ServiceGroupId = 5 },
        //            new AllowedAccuralMethod { ServiceGroupId = 6 },
        //        }
        //    });

        //    db.AccuralMethods.Add(new AccuralMethod
        //    {
        //        Code = "OB_SUM_KVM",
        //        Description = "Общая сумма услуги, разбиваемая по кв.м.",
        //        AllowedAccuralMethods = new List<AllowedAccuralMethod>
        //        {
        //            new AllowedAccuralMethod { ServiceGroupId = 1 },
        //            new AllowedAccuralMethod { ServiceGroupId = 3 },
        //            new AllowedAccuralMethod { ServiceGroupId = 6 },
        //        }
        //    });

        //    db.AccuralMethods.Add(new AccuralMethod
        //    {
        //        Code = "OB_SUM_FLAT",
        //        Description = "Общая сумма услуги, разбиваемая равными частями по помещениям",
        //        AllowedAccuralMethods = new List<AllowedAccuralMethod>
        //        {
        //            new AllowedAccuralMethod { ServiceGroupId = 1 },
        //            new AllowedAccuralMethod { ServiceGroupId = 3 },
        //            new AllowedAccuralMethod { ServiceGroupId = 6 },
        //        }
        //    });

        //    await db.SaveChangesAsync();
        //}

        //public static async Task ProcessTxtFile(string filename)
        //{
        //    Dictionary<F, int> mapping = new Dictionary<F, int>
        //    {
        //        { F.OsiId, 0 },
        //        { F.AbonentId, 1 },
        //        { F.ServiceCode, 2 },
        //        { F.GroupId, 3 },
        //        { F.ServiceName, 4 },
        //        { F.Method, 5 },
        //        { F.Tarif, 6 },
        //        { F.Rezerv, 7 },
        //    };

        //    string Get(string[] fields, F code) => fields[mapping[code]];

        //    using var db = OSIBillingDbContext.DbContext;

        //    var osies = await db.Osies.ToListAsync();
        //    Osi osi = null;
        //    var accuralMethods = await db.AccuralMethods.ToListAsync();

        //    var lines = File.ReadAllLines(filename);
        //    ServiceCodes osiServiceServiceCode = ServiceCodes.OTHER;

        //    using (var dbTransaction = await db.Database.BeginTransactionAsync())
        //    {
        //        int i = 0;
        //        try
        //        {
        //            int osiServiceId = 0;
        //            string old_osiId = "0";
        //            string old_serviceCode = "";
        //            string old_serviceName = "";
        //            foreach (string line in lines.Skip(1))
        //            {
        //                i++;
        //                string[] fields = line.Split('\t');

        //                if (fields.Length < 8)
        //                    throw new Exception($"Строка {i}: меньшее кол-во слов");

        //                string osiId = Get(fields, F.OsiId);

        //                if (osiId != old_osiId)
        //                {
        //                    old_osiId = osiId;
        //                    old_serviceCode = "";
        //                    old_serviceName = "";
        //                    osi = (await db.Osies.FirstOrDefaultAsync(o => o.Id == Convert.ToInt32(osiId))) ?? throw new Exception("Оси не найден");
        //                }

        //                string serviceCode = Get(fields, F.ServiceCode);
        //                string serviceName = Get(fields, F.ServiceName);

        //                // новый код или наименование услуги
        //                if (serviceCode != old_serviceCode || serviceName != old_serviceName)
        //                {
        //                    old_serviceCode = serviceCode;
        //                    old_serviceName = serviceName;

        //                    // ищем по наименованию
        //                    var osiService = await db.OsiServices.FirstOrDefaultAsync(o => o.ServiceCode == Enum.Parse<ServiceCodes>(serviceCode)
        //                        && o.OsiId == osi.Id
        //                        && o.NameRu == serviceName);

        //                    if (osiService == null)
        //                    {
        //                        // ищем без наименования
        //                        osiService = await db.OsiServices.FirstOrDefaultAsync(o => o.ServiceCode == Enum.Parse<ServiceCodes>(serviceCode)
        //                            && o.OsiId == osi.Id
        //                            && string.IsNullOrEmpty(o.NameRu));

        //                        if (osiService == null)
        //                        {
        //                            var newOsiService = new OsiService
        //                            {
        //                                IsActive = true,
        //                                OsiId = osi.Id,
        //                                ServiceCode = Enum.Parse<ServiceCodes>(serviceCode),
        //                                NameRu = serviceName,
        //                                NameKz = serviceName,
        //                                ServiceGroupId = Convert.ToInt32(Get(fields, F.GroupId)),
        //                                IsOsibilling = serviceCode == "USSIKING"
        //                            };
        //                            db.OsiServices.Add(newOsiService);
        //                            await db.SaveChangesAsync();
        //                            osiServiceId = newOsiService.Id;
        //                            osiServiceServiceCode = newOsiService.ServiceCode.Value;
        //                        }
        //                        else
        //                        {
        //                            osiService.NameRu = serviceName;
        //                            osiService.NameKz = serviceName;
        //                            osiService.ServiceGroupId = Convert.ToInt32(Get(fields, F.GroupId));
        //                            osiService.IsOsibilling = serviceCode == "USSIKING";
        //                            db.OsiServices.Update(osiService);
        //                            osiServiceId = osiService.Id;
        //                            osiServiceServiceCode = osiService.ServiceCode.Value;
        //                        }
        //                    }
        //                    else throw new Exception("Почему-то нашлась услуга. Неверная сортировка в экселе?");

        //                    decimal tarif = Convert.ToDecimal(Get(fields, F.Tarif));
        //                    string method = Get(fields, F.Method);
        //                    string accuralMethodCodeToFind = method.ToUpper() switch
        //                    {
        //                        "ТАРИФ ЗА 1 КВ.М." => "TARIF_1KVM",
        //                        "ФИКСИРОВАННАЯ СУММА С ПОМЕЩЕНИЯ" => "FIX_SUM_FLAT",
        //                        _ => method
        //                    };

        //                    var osiServiceAmount = new OsiServiceAmount
        //                    {
        //                        Amount = tarif,
        //                        Dt = DateTime.Today,
        //                        OsiId = osi.Id,
        //                        OsiServiceId = osiServiceId,
        //                        AccuralMethodId = accuralMethods.FirstOrDefault(s => s.Code == accuralMethodCodeToFind)?.Id ?? 0,
        //                        Note = "Настройки при переносе данных"
        //                    };
        //                    await PeriodicDataLogic.SaveOsiServiceAmount(db, osiServiceAmount);
        //                }

        //                int abonentId = Convert.ToInt32(Get(fields, F.AbonentId));
        //                Abonent abonent = (await db.Abonents.FirstOrDefaultAsync(a => a.Id == abonentId)) ?? throw new Exception("Абонент не найден");
        //                string rezerv = Get(fields, F.Rezerv);
        //                bool isActive = rezerv != "Не переносить";

        //                var connectedService = new ConnectedService
        //                {
        //                    AbonentId = abonent.Id,
        //                    OsiId = osi.Id,
        //                    OsiServiceId = osiServiceId,
        //                    Dt = DateTime.Today,
        //                    IsActive = isActive,
        //                    Note = "Настройки при переносе данных"
        //                };
        //                await PeriodicDataLogic.SaveConnectedService(db, connectedService);

        //                // парковка
        //                if (osiServiceServiceCode == ServiceCodes.PARKING2)
        //                {
        //                    var parkingPlace = new ParkingPlace
        //                    {
        //                        AbonentId = abonent.Id,
        //                        OsiId = osi.Id,
        //                        OsiServiceId = osiServiceId,
        //                        Dt = DateTime.Today,
        //                        Places = abonent.ParkingPlaces2,
        //                        Note = "Настройки при переносе данных"
        //                    };
        //                    await PeriodicDataLogic.SaveParkingPlaces(db, parkingPlace);
        //                }
        //                await db.SaveChangesAsync();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            await dbTransaction.RollbackAsync();
        //            throw new Exception($"Строка {i}: {ex.Message}");
        //        }
        //        await dbTransaction.CommitAsync();
        //    }
        //}

        public static async Task ProcessTxtFile(string filename)
        {
            Dictionary<F, int> mapping = new Dictionary<F, int>
            {
                { F.OsiId, 0 },
                { F.Fio, 1 },
                { F.Flat, 4 },
                { F.Square, 5 },
                { F.Resident, 6 },
                { F.Iin, 7 },
                { F.Phone, 8 },
                { F.Saldo, 9 },
            };

            string Get(string[] fields, F code) => fields[mapping[code]];

            using var db = OSIBillingDbContext.DbContext;

            var osies = await db.Osies.ToListAsync();
            Osi osi = null;

            var lines = File.ReadAllLines(filename);


            int i = 0;

            List<TempClass> tempList = new List<TempClass>();

            try
            {
                foreach (string line in lines.Skip(1))
                {
                    i++;
                    string[] fields = line.Split('\t');

                    if (fields.Length < 10)
                        throw new Exception($"Строка {i}: меньшее кол-во слов");

                    var t = new TempClass
                    {
                        OsiId = Convert.ToInt32(Get(fields, F.OsiId)),
                        Fio = Get(fields, F.Fio),
                        Flat = Get(fields, F.Flat),
                        Iin = Get(fields, F.Iin),
                        Phone = Get(fields, F.Phone),
                        Square = decimal.Parse(Get(fields, F.Square)),
                        AreaTypeCode = Get(fields, F.Resident) switch
                        {
                            "0" => AreaTypeCodes.RESIDENTIAL,
                            "1" => AreaTypeCodes.NON_RESIDENTIAL,
                            _ => AreaTypeCodes.RESIDENTIAL
                        },
                        Saldo = decimal.Parse(Get(fields, F.Saldo)),
                    };
                    //if (t.OsiId != 274)
                    //    break;
                    tempList.Add(t);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Строка {i}: {ex.Message}");
            }

            using (var dbTransaction = await db.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var g in tempList.Where(a => a.OsiId != 274).GroupBy(a => a.OsiId))
                    {
                        osi = (await db.Osies.FirstOrDefaultAsync(o => o.Id == Convert.ToInt32(g.Key))) ?? throw new Exception("Оси не найден");

                        // удаляем заведенных абонентов
                        var abonents = await db.Abonents.Where(o => o.OsiId == osi.Id).ToListAsync();
                        db.Abonents.RemoveRange(abonents);
                        await db.SaveChangesAsync();

                        // услуги
                        OsiService osiServiceFiz = new OsiService
                        {
                            IsActive = true,
                            OsiId = osi.Id,
                            NameRu = "Техническое обслуживание",
                            NameKz = "Техникалық қызмет көрсету",
                            ServiceGroupId = 1,
                            IsOsibilling = false
                        };

                        OsiService osiServiceJur = new OsiService
                        {
                            IsActive = true,
                            OsiId = osi.Id,
                            NameRu = "Техническое обслуживание (юр.лица)",
                            NameKz = "Техникалық қызмет көрсету (заңды тұлға)",
                            ServiceGroupId = 1,
                            IsOsibilling = false
                        };
                        db.OsiServices.Add(osiServiceFiz);
                        db.OsiServices.Add(osiServiceJur);
                        await db.SaveChangesAsync();

                        // тарифы
                        var osiServiceAmountFiz = new OsiServiceAmount
                        {
                            OsiId = osi.Id,
                            AccuralMethodId = 1,
                            Dt = DateTime.Today,
                            Note = "загрузка из файла",
                            OsiServiceId = osiServiceFiz.Id
                        };

                        var osiServiceAmountJur = new OsiServiceAmount
                        {
                            OsiId = osi.Id,
                            AccuralMethodId = 1,
                            Dt = DateTime.Today,
                            Note = "загрузка из файла",
                            OsiServiceId = osiServiceJur.Id
                        };

                        osiServiceAmountFiz.Amount = osi.Id == 254 ? 27 : 30;
                        osiServiceAmountJur.Amount = 55;
                        db.OsiServiceAmounts.Add(osiServiceAmountFiz);
                        db.OsiServiceAmounts.Add(osiServiceAmountJur);

                        // счета
                        var osiAccount = new OsiAccount
                        {
                            OsiId = osi.Id,
                            AccountTypeCode = AccountTypeCodes.CURRENT,
                            BankBic = "CASPKZKA",
                            Account = "KZ19722S000006752349"
                        };
                        db.OsiAccounts.Add(osiAccount);
                        await db.SaveChangesAsync();

                        foreach (TempClass t in g)
                        {
                            // добавляем абонента
                            var abonent = new Abonent()
                            {
                                OsiId = osi.Id,
                                Floor = 1,
                                Owner = "Собственник",
                                LivingFact = 1,
                                LivingJur = 1,
                                External = false,
                                Name = t.Fio,
                                AreaTypeCode = t.AreaTypeCode,
                                Flat = t.Flat,
                                Square = t.Square,
                                Idn = t.Iin,
                                Phone = t.Phone
                            };
                            db.Abonents.Add(abonent);
                            await db.SaveChangesAsync();

                            var connectedService = new ConnectedService
                            {
                                AbonentId = abonent.Id,
                                OsiId = osi.Id,
                                OsiServiceId = abonent.AreaTypeCode == AreaTypeCodes.RESIDENTIAL ? osiServiceFiz.Id : osiServiceJur.Id,
                                Dt = DateTime.Today,
                                IsActive = true,
                                Note = "загрузка из файла"
                            };
                            db.ConnectedServices.Add(connectedService);

                            // долги
                            var serviceGroupSaldo = new ServiceGroupSaldo
                            {
                                Saldo = t.Saldo,
                                GroupId = 1,
                                AbonentId = abonent.Id,
                                OsiId = t.OsiId,
                                Transaction = new Transaction
                                {
                                    AbonentId = abonent.Id,
                                    Dt = new DateTime(1, 1, 1),
                                    Amount = t.Saldo,
                                    OsiId = t.OsiId,
                                    GroupId = 1,
                                    TransactionType = TransactionTypeCodes.SALDO
                                }
                            };
                            db.ServiceGroupSaldos.Add(serviceGroupSaldo);

                            await db.SaveChangesAsync();
                        }

                        // меняем кол-во квартир и wizardstep
                        osi.ApartCount = g.Count();
                        osi.WizardStep = "finish";
                        db.Entry(osi).Property(a => a.ApartCount).IsModified = true;
                        db.Entry(osi).Property(a => a.WizardStep).IsModified = true;

                        await db.SaveChangesAsync();
                    }
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
                await dbTransaction.CommitAsync();
            }
        }

        //public static async Task ProcessOSIBillingServices()
        //{
        //    using var db = OSIBillingDbContext.DbContext;

        //    using (var dbTransaction = await db.Database.BeginTransactionAsync())
        //    {
        //        try
        //        {
        //            var accuralMethod = await db.AccuralMethods.FirstOrDefaultAsync(a => a.Code == "FIX_SUM_FLAT");
        //            var osiBillingServices = await db.OsiServices.Where(o => o.IsOsibilling).ToListAsync();
        //            foreach (var service in osiBillingServices)
        //            {
        //                bool existsAmounts = await db.OsiServiceAmounts.AnyAsync(o => o.OsiServiceId == service.Id);
        //                if (!existsAmounts)
        //                {
        //                    var tariff = await OsiTariffLogic.GetOsiTariffValueByDate(service.OsiId, DateTime.Today);
        //                    await PeriodicDataLogic.SaveOsiServiceAmount(db, new OsiServiceAmount
        //                    {
        //                        Amount = tariff,
        //                        Dt = DateTime.Today,
        //                        OsiId = service.OsiId,
        //                        OsiServiceId = service.Id,
        //                        AccuralMethodId = accuralMethod.Id,
        //                        Note = "добавление суммы"
        //                    }, true);
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            await dbTransaction.RollbackAsync();
        //            throw;
        //        }
        //        await dbTransaction.CommitAsync();
        //    }
        //}

    }
}
