using Aspose.Cells;
using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using OSI.Core.Models.ExcelAbonents;
using OSI.Core.Models.Requests;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace OSI.Core.Logic
{
    public class ExcelAbonentsLoader
    {
        private const string ESTR_FLAT = "Номер квартиры";
        private const string ESTR_SQUARE_ALL = "Площадь|Общая";
        private const string ESTR_SQUARE_USEFUL = "Площадь|Полезная";
        private const string ESTR_IS_LIVING = "Жилые/Нежилые";
        private const string ESTR_DEBT = "Долг на конец месяца";
        private readonly IServiceGroupSaldoSvc serviceGroupSaldoSvc;
        private readonly Osi osi;

        private bool IsNull(string s) => string.IsNullOrWhiteSpace(s);

        private string GetErrOnIndex(int index, string error) => $"Строка {index + 1}: {error}";

        private string GetErrNullValue(int index, string fieldName) => GetErrOnIndex(index, $"Пустое значение в поле \"{fieldName}\"");

        private string GetErrFormatValue(int index, string fieldName, string fieldValue) => GetErrOnIndex(index, $"Неверное значение в поле \"{fieldName}\": \"{fieldValue}\"");

        bool ToDecimal(string value, out decimal d)
        {
            string wantedDecimalSeparator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
            string alternateDecimalSeparator = wantedDecimalSeparator == "," ? "." : ",";

            if (value.IndexOf(wantedDecimalSeparator, StringComparison.Ordinal) == -1 && value.IndexOf(alternateDecimalSeparator, StringComparison.Ordinal) != -1)
            {
                value = value.Replace(alternateDecimalSeparator, wantedDecimalSeparator);
            }

            try
            {
                d = Convert.ToDecimal(string.IsNullOrEmpty(value) ? "0" : value);
                return true;
            }
            catch
            {
                d = 0;
                return false;
            }
        }

        public (bool Success, string ErrorMessage, List<ParsedAbonent> Abonents) ReadExcelFile(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                using (var wb = new Workbook(stream))
                {
                    Worksheet ws = wb.Worksheets[0];
                    Cells cells = ws.Cells;
                    int maxrow = cells.MaxDataRow;
                    int row = 1; // пропуск первой строки, начинается с 0
                    var abonents = new List<ParsedAbonent>();
                    string tmp = "";
                    while (row <= maxrow)
                    {
                        tmp = Convert.ToString(cells[row, 0].Value).Trim();
                        if (IsNull(tmp))
                            return (false, GetErrNullValue(row, ESTR_FLAT), null);

                        if (tmp == "0")
                        {
                            row++;
                            continue;
                        }

                        var a = new ParsedAbonent();
                        a.Flat = tmp;

                        tmp = Convert.ToString(cells[row, 1].Value);
                        if (IsNull(tmp))
                            return (false, GetErrNullValue(row, ESTR_SQUARE_ALL), null);

                        if (!ToDecimal(tmp, out var d) || d < 0)
                            return (false, GetErrFormatValue(row, ESTR_SQUARE_ALL, tmp), null);

                        a.Square = d;

                        tmp = Convert.ToString(cells[row, 2].Value);
                        if (IsNull(tmp))
                            return (false, GetErrNullValue(row, ESTR_SQUARE_USEFUL), null);
                        if (!ToDecimal(tmp, out d) || d < 0)
                            return (false, GetErrFormatValue(row, ESTR_SQUARE_USEFUL, tmp), null);

                        a.EffectiveSquare = d;

                        tmp = Convert.ToString(cells[row, 3].Value);

                        if (IsNull(tmp))
                            return (false, GetErrNullValue(row, ESTR_IS_LIVING), null);
                        if (!int.TryParse(tmp, out var isLiving) || (isLiving != 0 && isLiving != 1))
                            return (false, GetErrFormatValue(row, ESTR_IS_LIVING, tmp), null);

                        a.AreaTypeCode = isLiving == 0 ? AreaTypeCodes.RESIDENTIAL : AreaTypeCodes.NON_RESIDENTIAL;

                        tmp = Convert.ToString(cells[row, 4].Value);
                        if (IsNull(tmp))
                            return (false, GetErrNullValue(row, ESTR_DEBT), null);
                        if (!ToDecimal(tmp, out d))
                            return (false, GetErrFormatValue(row, ESTR_DEBT, tmp), null);

                        a.Debt = d;

                        abonents.Add(a);

                        row++;
                    }
                    return (true, "", abonents);
                }
            }
        }

        public async Task<(bool IsSuccess, string ErrorMessage, List<Abonent> Abonents)> LoadAbonents(IServiceGroupSaldoSvc serviceGroupSaldoSvc,
                                                                                                      List<ParsedAbonent> parsedAbonents,
                                                                                                      Osi osi,
                                                                                                      OSIBillingDbContext db)
        {
            using var dbTransaction = await db.Database.BeginTransactionAsync();
            bool fineExist = await db.Transactions.AnyAsync(a => a.OsiId == osi.Id && a.TransactionType == TransactionTypeCodes.FINE);
            if (fineExist)
            {
                return (false, "Уже проводилось начисление пени, поэтому изменение начального сальдо невозможно.", null);
            }

            var existingAbonents = await db.Abonents.Where(a => a.OsiId == osi.Id).ToListAsync();
            try
            {
                var abonents = new List<Abonent>();
                foreach (ParsedAbonent pa in parsedAbonents)
                {
                    // добавляем/правим данные абонента
                    bool isNew = false;
                    var existAbonent = existingAbonents.FirstOrDefault(a => a.Flat == pa.Flat);
                    if (existAbonent == null)
                    {
                        isNew = true;
                        existAbonent = new Abonent
                        {
                            OsiId = osi.Id,
                            OsiName = osi.Name,
                            Flat = pa.Flat,
                        };
                    }
                    existAbonent.AreaTypeCode = pa.AreaTypeCode;
                    existAbonent.EffectiveSquare = pa.EffectiveSquare;
                    existAbonent.Square = pa.Square;
                    existAbonent.Owner = "Собственник";
                    existAbonent.IsActive = true;

                    if (isNew)
                    {
                        db.Abonents.Add(existAbonent);
                    }
                    else
                    {
                        db.Abonents.Update(existAbonent);
                    }
                    await db.SaveChangesAsync();
                    abonents.Add(existAbonent);

                    if (pa.Debt == 0)
                        continue;

                    // добавляем/правим сальдо на начало
                    ServiceGroupSaldo saldo = null;
                    if (!isNew)
                    {
                        saldo = await db.ServiceGroupSaldos
                            .Include(s => s.Transaction)
                            .FirstOrDefaultAsync(s => s.OsiId == osi.Id && s.AbonentId == existAbonent.Id && s.GroupId == 1);
                    }

                    if (isNew || saldo == null)
                    {
                        db.ServiceGroupSaldos.Add(new ServiceGroupSaldo
                        {
                            AbonentId = existAbonent.Id,
                            GroupId = 1,
                            OsiId = osi.Id,
                            Saldo = pa.Debt,
                            Transaction = new Transaction
                            {
                                AbonentId = existAbonent.Id,
                                Dt = new DateTime(1, 1, 1),
                                Amount = pa.Debt,
                                OsiId = osi.Id,
                                GroupId = 1,
                                TransactionType = TransactionTypeCodes.SALDO
                            }
                        });
                    }
                    else
                    {
                        saldo.Saldo = pa.Debt;
                        saldo.Transaction.Amount = pa.Debt;
                        db.ServiceGroupSaldos.Update(saldo);
                    }
                    await db.SaveChangesAsync();
                }
                await dbTransaction.CommitAsync();
                return (true, "", abonents);
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return (false, ex.InnerException?.Message ?? ex.Message, null);
            }
        }
    }
}
