using OSI.Core.Models.AccountReports;
using System.Collections.Generic;
using System;
using Aspose.Cells;
using System.IO;
using OSI.Core.Models.Enums;

namespace OSI.Core.Logic.BankStatementParsing
{
    public class BccBankExcelStatementParser : IStatementParser
    {
        public bool CheckFileFormat(byte[] data)
        {
            if (data.Length == 0)
                return false;

            return (data[0] == 208 && data[1] == 207 && data[2] == 17);
        }

        public BankStatement ParseData(byte[] data)
        {
            BankStatement bs = new BankStatement();
            using (MemoryStream stream = new MemoryStream(data))
            {
                Workbook wb = new Workbook(stream);
                Worksheet ws = wb.Worksheets[0];
                Cells cells = ws.Cells;
                
                bs.Account = Convert.ToString(cells[4, 0].Value);
                bs.Account = bs.Account.Substring(bs.Account.IndexOf("KZ"), 20);

                string tmp = Convert.ToString(cells[5, 7].Value).Replace("Движения по счету за ", "").Replace(" по ", ",");
                string[] d = tmp.Split(",");
                if (!DateTime.TryParse(d[0], out DateTime dt))
                    throw new Exception("Ошибка при проверке даты начала выписки: Неверный формат даты");
                bs.PeriodStart = dt;
                if (!DateTime.TryParse(d[1], out dt))
                    throw new Exception("Ошибка при проверке даты конца выписки: Неверный формат даты");
                bs.PeriodEnd = dt;

                bs.Begin = decimal.Parse(Convert.ToString(cells[7, 4].Value).Replace(",", "").Replace(".", ","));
                int row = 9;
                tmp = Convert.ToString(cells[row, 0].Value);
                bs.Items = new List<BankStatementItem>();
                while (!string.IsNullOrEmpty(tmp))
                {
                    var bi = new BankStatementItem();
                    // проверим графу сумма
                    if (!string.IsNullOrEmpty(Convert.ToString(cells[row, 7].Value)))
                        bi.OperationTypeCode = OperationTypeCodes.DEBET;
                    else if (!string.IsNullOrEmpty(Convert.ToString(cells[row, 8].Value)))
                        bi.OperationTypeCode = OperationTypeCodes.KREDIT;
                    else throw new Exception("Не указана сумма операции");

                    tmp = Convert.ToString(cells[row, 1].Value).Trim();
                    if (!DateTime.TryParse(tmp, out DateTime dt1))
                        throw new Exception("Ошибка при проверке даты очередного документа: Неверный формат даты");

                    bi.Dt = dt1;
                    bi.SenderBin = Convert.ToString(cells[row, 4].Value).Trim();
                    bi.ReceiverBin = Convert.ToString(cells[row, 6].Value).Trim();
                    if (bi.OperationTypeCode == OperationTypeCodes.DEBET)
                    {
                        bi.Sender = Convert.ToString(cells[1, 0].Value).Trim().Substring(19);  // строка 2
                        bi.Receiver = Convert.ToString(cells[row, 5].Value).Trim();  // столбец F
                        bi.Amount = decimal.Parse(Convert.ToString(cells[row, 7].Value).Replace(",", "").Replace(".", ","));
                    }
                    else
                    {
                        bi.Sender = Convert.ToString(cells[row, 5].Value).Trim();   // столбец F
                        bi.Receiver = Convert.ToString(cells[1, 0].Value).Trim().Substring(19);   // строка 2
                        bi.Amount = decimal.Parse(Convert.ToString(cells[row, 8].Value).Replace(",", "").Replace(".", ","));
                    }
                    bi.Assign = Convert.ToString(cells[row, 11].Value).Trim();
                    bs.Items.Add(bi);

                    row++;
                    tmp = Convert.ToString(cells[row, 0].Value);
                }
                bs.Kredit = decimal.Parse(Convert.ToString(cells[row, 7].Value).Replace(",", "").Replace(".", ",")); // в экселе наоборот - берется с графы Дебет
                bs.Debet = decimal.Parse(Convert.ToString(cells[row, 8].Value).Replace(",", "").Replace(".", ","));  // в экселе наоборот - берется с графы Кредит
                row = row + 2;
                bs.End = decimal.Parse(Convert.ToString(cells[row, 4].Value).Replace(",", "").Replace(".", ","));
            }

            return bs;
        }
    }
}
