using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using OSI.Core.Models.Reports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;
using OSI.Core.Models.Enums;
using System.Globalization;
using OfficeOpenXml.Style;
using OSI.Core.Logic;
using SocialExplorer.IO.FastDBF;
using OSI.Core.Models;
using OSI.Core.Pages;

namespace OSI.Core.Services
{
    public interface IReportsSvc
    {
        Task<string> GetDebtFile(DateTime onDate1, DateTime onDate2, int osiId, DebtFileTypeCodes debtFileType);
        Task<string> GetAllInOneDebtFile(DateTime onDate1, DateTime onDate2, DebtFileTypeCodes debtFileType);

        Task<string> FileForCheckActs(DateTime onDate);

        Task<string> OsiesInformation();

        Task<string> SvodPaymentOrders(DateTime onDate1, DateTime onDate2, int osiId);

        Task<ApiResponse<string>> GetPaymentOrdersDBFKazPost(DateTime onDate);
    }

    public class ReportsSvc : IReportsSvc
    {
        private readonly IOsiSvc osiSvc;
        private readonly ITransactionSvc transactionSvc;
        private readonly IPaymentOrderSvc paymentOrderSvc;
        private readonly IWebHostEnvironment env;
        public string ReportsFolder => Path.Combine(env.WebRootPath, "reports");

        public ReportsSvc(IWebHostEnvironment env, IOsiSvc osiSvc, ITransactionSvc transactionSvc, IPaymentOrderSvc paymentOrderSvc)
        {
            this.osiSvc = osiSvc;
            this.transactionSvc = transactionSvc;
            this.paymentOrderSvc = paymentOrderSvc;
            this.env = env;
            Directory.CreateDirectory(ReportsFolder);
        }

        private string PrepareFile(string filename)
        {
            int filenameIndex = 1;
            string outfilename = Path.Combine(ReportsFolder, filename);
            while (File.Exists(outfilename))
            {
                outfilename = Path.Combine(ReportsFolder, Path.GetFileNameWithoutExtension(filename) + "(" + filenameIndex++ + ")" + Path.GetExtension(filename));
            }
            return outfilename;
        }

        /// <summary>
        /// Файл долгов по ОСИ для ЕРЦ
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetDebtFile(DateTime onDate1, DateTime onDate2, int osiId, DebtFileTypeCodes debtFileType)
        {
            var osi = await osiSvc.GetOsiById(osiId);
            var osv = await transactionSvc.GetOSVOnDateByOsiId(onDate1, onDate2, osiId);
            using var db = OSIBillingDbContext.DbContext;
            var osiAccounts = await db.OsiAccounts.Where(oa => oa.OsiId == osiId).ToListAsync();
            var groups = await db.ServiceGroups.Include(g => g.AccountType).Select(g => new
            {
                Num = g.Id,
                NameRu = g.NameRu,
                AccountTypeCode = g.AccountType.Code,
                AccountTypeNameRu = g.AccountType.NameRu,
            }).ToListAsync();

            var services = osv.Abonents.SelectMany(a => a.Services).GroupBy(s => s.ServiceName).Select(g => g.Key).Distinct().ToList();
            Dictionary<string, string> accounts = new();
            Dictionary<string, string> bics = new();
            foreach (string s in services)
            {
                var group = groups.FirstOrDefault(g => g.NameRu == s);
                if (group == null)
                    throw new Exception($"Услуга \"{s}\" не найдена");

                var osiAccount = await db.OsiAccounts.FirstOrDefaultAsync(oa => oa.OsiId == osiId && oa.AccountTypeCode == group.AccountTypeCode);
                if (osiAccount == null)
                    throw new Exception($"Счет \"{group.AccountTypeNameRu}\" не найден на данном ОСИ");

                accounts.Add(s, osiAccount.Account);
                bics.Add(s, osiAccount.BankBic);
            }

            char sep = ';';
            string filename = osi.Name.Replace("\"", "-").Replace("/", "-") + ".csv";
            string path = Path.Combine(ReportsFolder, filename);

            FileStream stream = new FileStream(path, FileMode.Create);
            using (StreamWriter writer = new StreamWriter(stream, (debtFileType == DebtFileTypeCodes.CSV_ANSI ? Encoding.GetEncoding("windows-1251") : Encoding.UTF8)))
            {
                var nfi = new NumberFormatInfo { NumberDecimalSeparator = "," };

                string line = "Лицевой счет" + sep;
                line += "Наименование ОСИ" + sep;
                line += "РКА" + sep;
                line += "Квартира" + sep;
                line += "Долг на начало" + sep;
                line += "Начислено" + sep;
                line += "Оплачено" + sep;
                line += "Долг на конец" + sep;
                line += "БИН" + sep;
                line += "Счет" + sep;
                line += "БИК" + sep;
                line += "Код услуги" + sep;
                line += "Наименование услуги" + sep;
                line += "OSI_ID";
                await writer.WriteLineAsync(line);

                foreach (OSVAbonent a in osv.Abonents/*.Where(e=>e.AreaTypeCode == AreaTypeCodes.NON_RESIDENTIAL).ToList()*/)
                {
                    foreach (OSVSaldo saldo in a.Services)
                    {
                        line = a.AbonentId.ToString() + sep;
                        line += osi.Name + sep;
                        line += osi.Rca + sep;
                        // OSI-136 
                        // добавлять букву Н или П только в том случае, если номер помещения пересекается с с таким же номером помещения в этом доме.
                        // Если пересечения нет то букву к не жилому помещению (или подвалу) не добавлять.
                        if (a.AreaTypeCode == AreaTypeCodes.NON_RESIDENTIAL || a.AreaTypeCode == AreaTypeCodes.BASEMENT)
                        {
                            if (osv.Abonents.Any(z => z.Flat == a.Flat && z.AreaTypeCode == AreaTypeCodes.RESIDENTIAL))
                            {
                                line += a.Flat + (a.AreaTypeCode switch
                                {
                                    AreaTypeCodes.NON_RESIDENTIAL => "Н",
                                    AreaTypeCodes.BASEMENT => "П",
                                    _ => ""
                                }) + sep;
                            }
                            else line += a.Flat + sep;
                        }
                        else line += a.Flat + sep;
                        line += saldo.Begin.ToString("F2", nfi) + sep;
                        line += saldo.Debet.ToString("F2", nfi) + sep;
                        line += saldo.Kredit.ToString("F2", nfi) + sep;
                        line += saldo.End.ToString("F2", nfi) + sep;
                        line += osi.Idn + sep;
                        line += accounts[saldo.ServiceName] + sep;
                        line += bics[saldo.ServiceName] + sep;
                        var group = groups.First(g => g.NameRu == saldo.ServiceName);
                        line += group.Num.ToString() + sep;
                        line += group.NameRu + sep;
                        line += osi.Id;
                        await writer.WriteLineAsync(line);
                    }
                }
            }

            return path;
        }

        /// <summary>
        /// Файл долгов по всем ОСИ для ЕРЦ
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetAllInOneDebtFile(DateTime onDate1, DateTime onDate2, DebtFileTypeCodes debtFileType)
        {
            static string writeError(Osi osi, string message) => $"{osi.Name} (id={osi.Id}): {message}" + Environment.NewLine;

            using var db = OSIBillingDbContext.DbContext;
            var osies = await db.Osies.Where(o => o.IsLaunched && o.IsActive).ToListAsync();
            string errors = "";
            var groups = await db.ServiceGroups.Include(g => g.AccountType).Select(g => new
            {
                Num = g.Id,
                NameRu = g.NameRu,
                AccountTypeCode = g.AccountType.Code,
                AccountTypeNameRu = g.AccountType.NameRu,
            }).ToListAsync();
            char sep = ';';
            string filename = "osiDebt_" + DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss") + ".csv";
            string path = Path.Combine(ReportsFolder, filename);

            FileStream stream = new FileStream(path, FileMode.Create);
            using (StreamWriter writer = new StreamWriter(stream, (debtFileType == DebtFileTypeCodes.CSV_ANSI ? Encoding.GetEncoding("windows-1251") : Encoding.UTF8)))
            {
                var nfi = new NumberFormatInfo { NumberDecimalSeparator = "," };

                string line = "Лицевой счет" + sep;
                line += "Наименование ОСИ" + sep;
                line += "РКА" + sep;
                line += "Квартира" + sep;
                line += "Долг на начало" + sep;
                line += "Начислено" + sep;
                line += "Оплачено" + sep;
                line += "Долг на конец" + sep;
                line += "БИН" + sep;
                line += "Счет" + sep;
                line += "БИК" + sep;
                line += "Код услуги" + sep;
                line += "Наименование услуги" + sep;
                line += "OSI_ID";
                writer.WriteLine(line);
                foreach (Osi osi in osies)
                {
                    var osiAccounts = await db.OsiAccounts.Where(oa => oa.OsiId == osi.Id).ToListAsync();
                    if (!osiAccounts?.Any() ?? true)
                    {
                        errors += writeError(osi, "Отсутствуют счета");
                        continue;
                    }
                    var osv = await transactionSvc.GetOSVOnDateByOsiId(onDate1, onDate2, osi.Id);
                    if (!osv.Abonents?.Any() ?? true)
                    {
                        errors += writeError(osi, "Отсутствуют абоненты");
                        continue;
                    }
                    var services = osv.Abonents.SelectMany(a => a.Services).GroupBy(s => s.ServiceName).Select(g => g.Key).Distinct().ToList();
                    if (!services?.Any() ?? true)
                    {
                        errors += writeError(osi, "Отсутствуют начисления");
                        continue;
                    }

                    Dictionary<string, string> accounts = new();
                    Dictionary<string, string> bics = new();
                    foreach (string s in services)
                    {
                        var group = groups.FirstOrDefault(g => g.NameRu == s);
                        if (group == null)
                            throw new Exception(writeError(osi, $"Услуга \"{s}\" не найдена"));

                        var osiAccount = await db.OsiAccounts.FirstOrDefaultAsync(oa => oa.OsiId == osi.Id && oa.AccountTypeCode == group.AccountTypeCode);
                        if (osiAccount == null)
                            throw new Exception(writeError(osi, $"Счет \"{group.AccountTypeNameRu}\" не найден на данном ОСИ"));

                        accounts.Add(s, osiAccount.Account);
                        bics.Add(s, osiAccount.BankBic);
                    }

                    foreach (OSVAbonent a in osv.Abonents/*.Where(e=>e.AreaTypeCode == AreaTypeCodes.NON_RESIDENTIAL).ToList()*/)
                    {
                        foreach (OSVSaldo saldo in a.Services)
                        {
                            line = a.AbonentId.ToString() + sep;
                            line += osi.Name + sep;
                            line += osi.Rca + sep;
                            // OSI-136 
                            // добавлять букву Н или П только в том случае, если номер помещения пересекается с с таким же номером помещения в этом доме.
                            // Если пересечения нет то букву к не жилому помещению (или подвалу) не добавлять.
                            if (a.AreaTypeCode == AreaTypeCodes.NON_RESIDENTIAL || a.AreaTypeCode == AreaTypeCodes.BASEMENT)
                            {
                                if (osv.Abonents.Any(z => z.Flat == a.Flat && z.AreaTypeCode == AreaTypeCodes.RESIDENTIAL))
                                {
                                    line += a.Flat + (a.AreaTypeCode switch
                                    {
                                        AreaTypeCodes.NON_RESIDENTIAL => "Н",
                                        AreaTypeCodes.BASEMENT => "П",
                                        _ => ""
                                    }) + sep;
                                }
                                else line += a.Flat + sep;
                            }
                            else line += a.Flat + sep;
                            line += saldo.Begin.ToString("F2", nfi) + sep;
                            line += saldo.Debet.ToString("F2", nfi) + sep;
                            line += saldo.Kredit.ToString("F2", nfi) + sep;
                            line += saldo.End.ToString("F2", nfi) + sep;
                            line += osi.Idn + sep;
                            line += accounts[saldo.ServiceName] + sep;
                            line += bics[saldo.ServiceName] + sep;
                            var group = groups.First(g => g.NameRu == saldo.ServiceName);
                            line += group.Num.ToString() + sep;
                            line += group.NameRu + sep;
                            line += osi.Id;
                            writer.WriteLine(line);
                        }
                    }
                }
                writer.WriteLine("");
                writer.WriteLine("Ошибки");
                writer.WriteLine(errors);
            }
            return path;
        }

        /// <summary>
        /// Файл для проверки данных по актам
        /// </summary>
        /// <param name="onDate">Дата актов - первое число предыдущего месяца</param>
        /// <returns></returns>
        public async Task<string> FileForCheckActs(DateTime onDate)
        {
            string filename = "acts_" + onDate.ToString("dd-MM-yyyy") + ".xlsx";
            string path = PrepareFile(filename); // Path.Combine(ReportsFolder, filename);
            using var ep = new ExcelPackage(new FileInfo(path));
            ExcelWorksheet ws = ep.Workbook.Worksheets.Add("acts");
            // заголовок ===================================================================
            (string Name, int Width)[] header = new (string, int)[]
            {
                ("OSI_ID", 10),
                ("Наименование ОСИ", 30),
                ("БИН", 13),
                ("Физ/Юр лицо", 13),
                ("Адрес", 30),
                ("Счет", 23),
                ("БИК", 11),
                ("Кол-во помещений", 11),
                ("Тариф", 11),
                ("Сумма ОСИ", 13),
                ("Сумма платежей", 13),
                ("Комиссия банка", 13),
                ("Дата договора", 13),
                ("Участник акции", 13),
                ("PLAN_ID", 10),
                ("Номер акта", 11),
                ("Сумма акта", 13),
                ("Сумма удержания", 13),
                //("Сумма акта доп услуги", 13),
                ("Дата акта", 13),
                ("Дата подписания", 13),
            };

            int colsCount = header.Count();
            int currentRow = 1;
            var cells = ws.Cells[currentRow, 1, currentRow, colsCount];
            cells.Value = "Данные для проверки актов";
            cells.Style.Font.Size = 11;
            cells.Style.Font.Bold = true;
            cells.Merge = true;
            cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // период ===================================================================
            currentRow++;
            cells = ws.Cells[currentRow, 1, currentRow, colsCount];
            cells.Value = onDate.ToString("dd-MM-yyyy");
            cells.Style.Font.Size = 10;
            cells.Merge = true;
            cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // колонки, шапка таблицы ===================================================================
            currentRow++;
            int col = 0;
            ws.Row(currentRow).Height = 45;
            foreach (var item in header)
            {
                col++;
                ws.Cells[currentRow, col].Value = item.Name;
                ws.Column(col).Width = item.Width;
            }
            ws.Cells[currentRow, 1, currentRow, colsCount].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[currentRow, 1, currentRow, colsCount].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            // выравнивание текста в заголовке ===================================================================
            cells = ws.Cells[currentRow, 1, currentRow, colsCount];
            cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cells.Style.WrapText = true;
            cells.Style.Font.Size = 10;
            cells.Style.Font.Bold = true;

            int dataRow = currentRow + 1;
            using var db = OSIBillingDbContext.DbContext;
            var osies = await db.Osies.Where(o => o.IsLaunched && o.IsActive).ToListAsync();
            foreach (Osi osi in osies.OrderBy(o => o.Id))
            {
                var contract = await db.OsiDocs.OrderByDescending(o => o.CreateDt).FirstOrDefaultAsync(o => o.OsiId == osi.Id && o.DocTypeCode == "SIGNED_CONTRACT");

                currentRow++;
                PlanAccural planAccural = await db.PlanAccurals.FirstOrDefaultAsync(p => p.OsiId == osi.Id && p.BeginDate == onDate.Date);

                col = 0;
                ws.Cells[currentRow, ++col].Value = osi.Id;
                ws.Cells[currentRow, ++col].Value = osi.Name;
                ws.Cells[currentRow, ++col].Value = osi.Idn;

                // OSI-312, 9 августа: И еще после колонки БИН, нужно добавить колонку “Юр/Физ лицо“
                // сечас делаю каждый месяц вручную такой формулой в Excel = ЕСЛИ(ПСТР(C5; 5; 1) = "4"; "Юридическое"; "Физическое")
                if (!string.IsNullOrEmpty(osi.Idn) && osi.Idn.Length >= 5)
                    ws.Cells[currentRow, ++col].Value = osi.Idn.Substring(4, 1) == "4" ? "Юридическое" : "Физическое";
                else
                    ws.Cells[currentRow, ++col].Value = "";

                ws.Cells[currentRow, ++col].Value = osi.Address;

                OsiAccount osiAccount = await db.OsiAccounts.FirstOrDefaultAsync(o => o.OsiId == osi.Id && o.AccountTypeCode == AccountTypeCodes.CURRENT);
                ws.Cells[currentRow, ++col].Value = osiAccount?.Account;
                ws.Cells[currentRow, ++col].Value = osiAccount?.BankBic;

                ws.Cells[currentRow, ++col].Value = planAccural?.ApartCount;
                ws.Cells[currentRow, ++col].Value = planAccural?.Tariff ?? 0;
                ws.Cells[currentRow, ++col].Value = (planAccural?.ApartCount ?? 0) * (planAccural?.Tariff ?? 0); // сумма ОСИ

                DateTime dateBegin = new DateTime(onDate.Year, onDate.Month, 1);
                DateTime dateEnd = onDate.AddMonths(1).AddDays(-1);  // последнее число месяца
                var payments = await db.Payments.Where(p => p.OsiId == osi.Id && p.RegistrationDate >= dateBegin.Date && p.RegistrationDate < dateEnd.Date.AddDays(1)).ToListAsync();
                ws.Cells[currentRow, ++col].Value = payments?.Sum(p => p.Amount) ?? 0;    // сумма платежей

                Act act = planAccural != null ? (await db.Acts.Include(a => a.PromoOperations).Include(a => a.ActItems).FirstOrDefaultAsync(a => a.PlanAccuralId == planAccural.Id)) : null;

                // телефонный разговор с Сашкой 07-05-2023, когда выяснили что в комиссию платежей попадает комиссия платежей за доп.услуги, а в комиссии акта они не сидят
                ws.Cells[currentRow, ++col].Value = act?.Comission; // комиссия банка (тут нет комиссии доп.услуг)

                // числовой формат
                cells = ws.Cells[currentRow, col - 2, currentRow, col];
                cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                cells.Style.Numberformat.Format = "#,##0.00";

                // OSI-284
                ws.Cells[currentRow, ++col].Value = contract?.CreateDt?.ToString("dd.MM.yyyy") ?? "";  // дата договора

                // участник акции
                ws.Cells[currentRow, ++col].Value = (act?.PromoOperations.Any() ?? false) ? "Да" : "Нет";

                ws.Cells[currentRow, ++col].Value = planAccural?.Id;
                ws.Cells[currentRow, ++col].Value = act?.ActNum;
                decimal dopUslugiSumma = 0;
                decimal actSkidka = 0;
                if (act != null)
                {
                    // телефонный разговор с Сашкой 07-05-2023, когда выяснили что в сумме акта не должна сидеть наша сумма за доп.услуги
                    //dopUslugiSumma = await db.ActItems.Where(a => a.ActId == act.Id && a.Description.StartsWith("За дополнительные услуги")).SumAsync(a => a.Amount);
                    // OSI-395 очередной раз не пошел отчет, разобравшись стало понятно, что это из-за скидок
                    if (act.ActItems.Any(a => a.Description.StartsWith("Скидка")))
                        actSkidka = act.ActItems.Where(a => a.Description.StartsWith("Скидка")).Sum(a => a?.Amount ?? 0);
                }
                ws.Cells[currentRow, ++col].Value = (act?.Amount ?? 0) - dopUslugiSumma; // сумма акта

                // OSI-312, 10 июля: В отчете в поле “Сумма удержания“ должна учитываться сумма удержания и по доп услугам. Сейчас она туда не входит. 
                // OSI-312, 9 августа: В поле “сумма удержания“ сейчас задваивается сумма доп услуг, нужно это исправить. теперь сумму по доп.услугам надо убрать из суммы удержания
                decimal summaUderzhaniya = (osi.TakeComission ? ((act?.Amount - act?.Comission) ?? 0) : act?.Amount) ?? 0; // сумма удержания                 
                summaUderzhaniya -= Math.Abs(actSkidka); // скидка идет с минусом, поэтому чтобы не запутаться делаем Math.Abs
                ws.Cells[currentRow, ++col].Value = summaUderzhaniya;

                // OSI-312, 10 июля: И еще надо добавить отдельное поле “Сумма акта доп услуги“ и там отдельно отражать сумму по доп услугам данного ОСИ
                //ws.Cells[currentRow, ++col].Value = dopUslugiSumma; // Сумма акта доп услуги

                // числовой формат
                cells = ws.Cells[currentRow, col - 3, currentRow, col];
                cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                cells.Style.Numberformat.Format = "#,##0.00";

                ws.Cells[currentRow, ++col].Value = act?.ActPeriod.ToString("dd.MM.yyyy"); // дата акта
                ws.Cells[currentRow, ++col].Value = act?.SignDt?.ToString("dd.MM.yyyy") ?? ""; // дата подписания
            }
            // рамки
            cells = ws.Cells[dataRow - 1, 1, currentRow, colsCount];
            cells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            cells.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            cells.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            cells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // шрифт основных данных
            cells = ws.Cells[dataRow, 1, currentRow, colsCount];
            cells.Style.Font.Size = 10;

            ws.PrinterSettings.PrintArea = ws.Cells[1, 1, currentRow, colsCount];
            ws.PrinterSettings.Orientation = eOrientation.Landscape;
            ws.PrinterSettings.TopMargin = 0.4m;
            ws.PrinterSettings.BottomMargin = 0.4m;
            ws.PrinterSettings.LeftMargin = 0.4m;
            ws.PrinterSettings.RightMargin = 0.4m;
            // сохранить док
            ep.Save();

            return path;
        }

        /// <summary>
        /// Сведения об ОСИ
        /// </summary>
        /// <returns></returns>
        public async Task<string> OsiesInformation()
        {
            string filename = "osies.xlsx";
            string path = PrepareFile(filename); // Path.Combine(ReportsFolder, filename);
            using var ep = new ExcelPackage(new FileInfo(path));
            ExcelWorksheet ws = ep.Workbook.Worksheets.Add("Список ОСИ");
            // заголовок ===================================================================
            int colsCount = 10;
            int currentRow = 1;
            var cells = ws.Cells[currentRow, 1, currentRow, colsCount];
            cells.Value = "Сведения об ОСИ";
            cells.Style.Font.Size = 11;
            cells.Style.Font.Bold = true;
            cells.Merge = true;
            cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // колонки, шапка таблицы ===================================================================
            currentRow++;
            (string Name, int Width)[] header = new (string, int)[]
            {
                ("Наименование ОСИ", 30),
                ("Адрес ОСИ", 35),
                ("Телефон председателя ОСИ", 13),
                ("ФИО председателя ОСИ", 30),
                ("Сервисные компании", 15),
                ("Контакты сервисных компаний", 15),
                ("БИН/ИИН ОСИ", 13),
                ("Текущий счет ОСИ", 20),
                ("БИК банка", 10),
                ("Кол-во помещений", 11),
            };
            int col = 0;
            ws.Row(currentRow).Height = 45;
            foreach (var item in header)
            {
                col++;
                ws.Cells[currentRow, col].Value = item.Name;
                ws.Column(col).Width = item.Width;
            }
            ws.Cells[currentRow, 1, currentRow, colsCount].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[currentRow, 1, currentRow, colsCount].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            // выравнивание текста в заголовке ===================================================================
            cells = ws.Cells[currentRow, 1, currentRow, colsCount];
            cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cells.Style.WrapText = true;
            cells.Style.Font.Size = 10;
            cells.Style.Font.Bold = true;

            int dataRow = currentRow + 1;
            using var db = OSIBillingDbContext.DbContext;
            var osies = await (from o in db.Osies
                               join oaa in db.OsiAccounts on o.Id equals oaa.OsiId into osia
                               from oa in osia.DefaultIfEmpty()
                               join oss in db.OsiServiceCompanies on o.Id equals oss.OsiId into osc
                               from os in osc.DefaultIfEmpty()
                               join c in db.ServiceCompanies on os.ServiceCompanyCode equals c.Code into cc
                               from sc in cc.DefaultIfEmpty()
                               where o.IsLaunched && o.IsActive && oa.AccountTypeCode == AccountTypeCodes.CURRENT
                               select new
                               {
                                   Name = o.Name,
                                   Address = o.Address,
                                   Phone = o.Phone,
                                   Fio = o.Fio,
                                   Company = sc.NameRu,
                                   CompPhones = os.Phones,
                                   Idn = o.Idn,
                                   Account = oa.Account,
                                   Bic = oa.BankBic,
                                   ApartCount = o.ApartCount
                               }).ToListAsync();

            foreach (var osi in osies)
            {
                currentRow++;
                ws.Cells[currentRow, 1].Value = osi.Name;
                ws.Cells[currentRow, 2].Value = osi.Address;
                ws.Cells[currentRow, 3].Value = osi.Phone;
                ws.Cells[currentRow, 4].Value = osi.Fio;
                ws.Cells[currentRow, 5].Value = osi.Company;
                ws.Cells[currentRow, 6].Value = osi.CompPhones;
                //var serviceCompanies = await db.OsiServiceCompanies.Where(o => o.OsiId == osi.Id).ToListAsync();
                //foreach (OsiServiceCompany oss in serviceCompanies)
                //{
                //    ws.Cells[currentRow, 5].Value = oss.ServiceCompanyNameRu;
                //    ws.Cells[currentRow, 6].Value = oss.Phones;

                //}
                ws.Cells[currentRow, 7].Value = osi.Idn;
                //var currentAccount = await db.OsiAccounts.FirstOrDefaultAsync(o => o.OsiId == osi.Id && o.AccountTypeCode == AccountTypeCodes.CURRENT);
                //if (currentAccount != null)
                //{
                //    ws.Cells[currentRow, 8].Value = currentAccount.Account;
                //    ws.Cells[currentRow, 9].Value = currentAccount.BankBic;
                //}
                ws.Cells[currentRow, 8].Value = osi.Account;
                ws.Cells[currentRow, 9].Value = osi.Bic;
                ws.Cells[currentRow, 10].Value = osi.ApartCount;
            }
            // рамки
            cells = ws.Cells[dataRow - 1, 1, currentRow, colsCount];
            cells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            cells.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            cells.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            cells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // шрифт основных данных
            cells = ws.Cells[dataRow, 1, currentRow, colsCount];
            cells.Style.Font.Size = 10;

            ws.PrinterSettings.PrintArea = ws.Cells[1, 1, currentRow, colsCount];
            ws.PrinterSettings.Orientation = eOrientation.Landscape;
            ws.PrinterSettings.TopMargin = 0.4m;
            ws.PrinterSettings.BottomMargin = 0.4m;
            ws.PrinterSettings.LeftMargin = 0.4m;
            ws.PrinterSettings.RightMargin = 0.4m;
            // сохранить док
            ep.Save();

            return path;
        }

        /// <summary>
        /// Сводный отчет по платежным поручениям
        /// </summary>
        /// <returns></returns>
        public async Task<string> SvodPaymentOrders(DateTime onDate1, DateTime onDate2, int osiId)
        {
            Osi osi = await osiSvc.GetOsiById(osiId);
            string period = onDate1 != onDate2 ? $"за период с {onDate1.ToString("dd/MM/yyyy")} по {onDate2.ToString("dd/MM/yyyy")}" : "за " + onDate1.ToString("dd/MM/yyyy");
            string filename = $"svod_{osiId}_{onDate1.ToString("dd-MM-yyyy")}_{onDate2.ToString("dd-MM-yyyy")}.xlsx";
            string path = PrepareFile(filename);
            using var ep = new ExcelPackage(new FileInfo(path));
            ExcelWorksheet ws = ep.Workbook.Worksheets.Add("Свод");
            // заголовок ===================================================================
            int colsCount = 7;
            int currentRow = 1;
            var cells = ws.Cells[currentRow, 1, currentRow, colsCount];
            cells.Value = $"Свод по платежным поручениям {osi.Name} {period}";
            cells.Style.Font.Size = 11;
            cells.Style.Font.Bold = true;
            cells.Merge = true;
            cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // колонки, шапка таблицы ===================================================================
            currentRow++;
            (string Name, int Width)[] header = new (string, int)[]
            {
                ("Источник", 15),
                ("Счет", 22),
                ("Дата", 13),
                ("Сумма платежей", 15),
                ("Комиссия банка", 15),
                ("Сумма ОСИ", 15),
                ("К перечислению", 15),
            };
            int col = 0;
            ws.Row(currentRow).Height = 45;
            foreach (var item in header)
            {
                col++;
                ws.Cells[currentRow, col].Value = item.Name;
                ws.Column(col).Width = item.Width;
            }
            ws.Cells[currentRow, 1, currentRow, colsCount].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[currentRow, 1, currentRow, colsCount].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            // выравнивание текста в заголовке ===================================================================
            cells = ws.Cells[currentRow, 1, currentRow, colsCount];
            cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cells.Style.WrapText = true;
            cells.Style.Font.Size = 10;
            cells.Style.Font.Bold = true;

            int dataRow = currentRow + 1;
            List<SvodPaymentOrder> paymentOrders = new List<SvodPaymentOrder>();
            var apiResult = await paymentOrderSvc.GetSvodPaymentOrdersByOsiId(osiId, onDate1, onDate2);
            if (apiResult.Code == 0)
            {
                paymentOrders = apiResult.Result.ToList();
            }
            else
            {
                throw new Exception(apiResult.Message);
            }

            foreach (var po in paymentOrders)
            {
                currentRow++;
                ws.Cells[currentRow, 1].Value = po.BankName;
                ws.Cells[currentRow, 2].Value = po.IBAN;
                ws.Cells[currentRow, 3].Value = po.Date.ToString("dd-MM-yyyy");
                ws.Cells[currentRow, 4].Value = po.Amount;
                ws.Cells[currentRow, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws.Cells[currentRow, 5].Value = po.ComisBank;
                ws.Cells[currentRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws.Cells[currentRow, 6].Value = po.ComisOur;
                ws.Cells[currentRow, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws.Cells[currentRow, 7].Value = po.AmountToTransfer;
                ws.Cells[currentRow, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            }

            // выводим итог
            currentRow++;
            cells = ws.Cells[currentRow, 1, currentRow, 3];
            cells.Value = $"ИТОГО";
            cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            cells.Merge = true;
            ws.Cells[currentRow, 4].Value = paymentOrders.Sum(po => po.Amount);
            ws.Cells[currentRow, 5].Value = paymentOrders.Sum(po => po.ComisBank);
            ws.Cells[currentRow, 6].Value = paymentOrders.Sum(po => po.ComisOur);
            ws.Cells[currentRow, 7].Value = paymentOrders.Sum(po => po.AmountToTransfer);
            cells = ws.Cells[currentRow, 1, currentRow, colsCount];
            cells.Style.Font.Bold = true;
            cells.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cells.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            cells = ws.Cells[dataRow, 4, currentRow, 7];
            cells.Style.Numberformat.Format = "#,##0.00";

            // рамки
            cells = ws.Cells[dataRow - 1, 1, currentRow, colsCount];
            cells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            cells.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            cells.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            cells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // шрифт основных данных
            cells = ws.Cells[dataRow, 1, currentRow, colsCount];
            cells.Style.Font.Size = 10;

            ws.PrinterSettings.PrintArea = ws.Cells[1, 1, currentRow, colsCount];
            ws.PrinterSettings.Orientation = eOrientation.Landscape;
            ws.PrinterSettings.TopMargin = 0.4m;
            ws.PrinterSettings.BottomMargin = 0.4m;
            ws.PrinterSettings.LeftMargin = 0.4m;
            ws.PrinterSettings.RightMargin = 0.4m;
            // сохранить док
            ep.Save();

            return path;
        }

        public async Task<ApiResponse<string>> GetPaymentOrdersDBFKazPost(DateTime onDate)
        {
            var result = new ApiResponse<string>();

            var getPaymentOrders = await paymentOrderSvc.GetPaymentOrders("KPST", onDate);
            if (getPaymentOrders.Code != 0)
            {
                result.Code = getPaymentOrders.Code;
                result.Message = getPaymentOrders.Message;
                return result;
            }

            try
            {
                var odbf = new DbfFile(Encoding.GetEncoding("cp866"));
                string dbfName = @$"AKTOBE_KPST_{onDate.ToString("ddMMyy")}.dbf";
                string dbfPath = Path.Combine(ReportsFolder, dbfName);
                odbf.Open(dbfPath, FileMode.Create);
                odbf.Header.AddColumn(new DbfColumn("DAT", DbfColumn.DbfColumnType.Date));
                odbf.Header.AddColumn(new DbfColumn("KODBN", DbfColumn.DbfColumnType.Character, 11, 0));
                odbf.Header.AddColumn(new DbfColumn("NUMRS", DbfColumn.DbfColumnType.Character, 20, 0));
                odbf.Header.AddColumn(new DbfColumn("TSH", DbfColumn.DbfColumnType.Character, 7, 0));
                odbf.Header.AddColumn(new DbfColumn("BIN", DbfColumn.DbfColumnType.Character, 12, 0));
                odbf.Header.AddColumn(new DbfColumn("SUMP", DbfColumn.DbfColumnType.Number, 14, 2));
                odbf.Header.AddColumn(new DbfColumn("NAME_POL", DbfColumn.DbfColumnType.Character, 40, 0));
                odbf.Header.AddColumn(new DbfColumn("NAZN_PL", DbfColumn.DbfColumnType.Character, 70, 0));
                odbf.Header.AddColumn(new DbfColumn("KODD", DbfColumn.DbfColumnType.Character, 6, 0));
                odbf.Header.AddColumn(new DbfColumn("FL", DbfColumn.DbfColumnType.Character, 1, 0));
                odbf.Header.AddColumn(new DbfColumn("KBE", DbfColumn.DbfColumnType.Character, 2, 0));
                odbf.Header.AddColumn(new DbfColumn("KNP", DbfColumn.DbfColumnType.Character, 3, 0));
                odbf.Header.AddColumn(new DbfColumn("IIN_ABON", DbfColumn.DbfColumnType.Character, 12, 0));
                odbf.Header.AddColumn(new DbfColumn("FIO", DbfColumn.DbfColumnType.Character, 40, 0));

                decimal comission = 0;
                foreach (var gpo in getPaymentOrders.Result)
                {
                    var orec = new DbfRecord(odbf.Header) { AllowDecimalTruncate = true };
                    orec["DAT"] = gpo.Date.ToString();
                    orec["KODBN"] = gpo.BIC;
                    orec["NUMRS"] = gpo.IBAN;
                    orec["BIN"] = gpo.IDN;
                    orec["SUMP"] = gpo.Amount.ToString().Replace(",", ".").Replace(" ", "");
                    orec["NAME_POL"] = gpo.Name;
                    orec["NAZN_PL"] = gpo.Assign;
                    orec["FL"] = "F";
                    orec["KBE"] = gpo.KBE;
                    orec["KNP"] = gpo.KNP;
                    odbf.Write(orec);
                    comission += gpo.BankCommission ?? 0;
                }

                if (comission > 0)
                {
                    var orec = new DbfRecord(odbf.Header) { AllowDecimalTruncate = true };
                    orec["DAT"] = onDate.ToString();
                    orec["KODBN"] = "KPSTKZKA";
                    orec["NUMRS"] = "KZ71563D601200019058";
                    orec["BIN"] = "000140002217";
                    orec["SUMP"] = comission.ToString().Replace(",", ".").Replace(" ", "");
                    orec["NAME_POL"] = "АО \"КАЗПОЧТА\"";
                    orec["NAZN_PL"] = "Комиссионные банка";
                    orec["FL"] = "F";
                    orec["KBE"] = "16";
                    orec["KNP"] = "841";
                    odbf.Write(orec);
                }
                odbf.WriteHeader();
                odbf.Close();
                result.Result = dbfPath;
            }
            catch (Exception ex)
            {
                result.Code = -1;
                result.Message = ex.Message;
            }

            return result;
        }
    }
}
