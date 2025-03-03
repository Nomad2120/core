using OSI.Core.Models.AccountReports;
using System.Collections.Generic;
using System;
using Aspose.Cells;
using System.IO;
using OSI.Core.Models.Enums;

namespace OSI.Core.Logic.BankStatementParsing
{
    public class FreedomBankStatementParser : IStatementParser
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

                bs.Account = Convert.ToString(cells[5, 0].Value);
                bs.Account = bs.Account.Substring(bs.Account.IndexOf("KZ"), 20);

                string tmp = Convert.ToString(cells[6, 0].Value).Replace("За период ", "").Replace(" - ", ",");
                string[] d = tmp.Split(",");
                if (!DateTime.TryParse(d[0], out DateTime dt))
                    throw new Exception("Ошибка при проверке даты начала выписки: Неверный формат даты");
                bs.PeriodStart = dt;
                if (!DateTime.TryParse(d[1], out dt))
                    throw new Exception("Ошибка при проверке даты конца выписки: Неверный формат даты");
                bs.PeriodEnd = dt;

                bs.Begin = decimal.Parse(Convert.ToString(cells[13, 0].Value).Replace("Входящий остаток ", "").Replace(".", ","));
                bs.End = decimal.Parse(Convert.ToString(cells[14, 0].Value).Replace("Исходящий остаток ", "").Replace(".", ","));

                var orgName = Convert.ToString(cells[8, 0].Value).Trim()[7..];
                var orgBin = Convert.ToString(cells[9, 0].Value).Trim()[8..];

                int row = 17;
                tmp = Convert.ToString(cells[row, 0].Value);
                bs.Items = new List<BankStatementItem>();
                while (!string.IsNullOrEmpty(tmp))
                {
                    var bi = new BankStatementItem();
                    // проверим графу сумма
                    if (!string.IsNullOrEmpty(Convert.ToString(cells[row, 8].Value)))
                        bi.OperationTypeCode = OperationTypeCodes.DEBET;
                    else if (!string.IsNullOrEmpty(Convert.ToString(cells[row, 9].Value)))
                        bi.OperationTypeCode = OperationTypeCodes.KREDIT;
                    else throw new Exception("Не указана сумма операции");

                    tmp = Convert.ToString(cells[row, 1].Value).Trim();
                    if (!DateTime.TryParse(tmp, out DateTime dt1))
                        throw new Exception("Ошибка при проверке даты очередного документа: Неверный формат даты");

                    bi.Dt = dt1;
                    if (bi.OperationTypeCode == OperationTypeCodes.DEBET)
                    {
                        bi.Sender = orgName;
                        bi.SenderBin = orgBin;
                        bi.Receiver = Convert.ToString(cells[row, 5].Value).Trim();
                        bi.ReceiverBin = Convert.ToString(cells[row, 6].Value).Trim();
                        bi.Amount = decimal.Parse(Convert.ToString(cells[row, 8].Value));
                    }
                    else
                    {
                        bi.Sender = Convert.ToString(cells[row, 5].Value).Trim();
                        bi.SenderBin = Convert.ToString(cells[row, 6].Value).Trim();
                        bi.Receiver = orgName;
                        bi.ReceiverBin = orgBin;
                        bi.Amount = decimal.Parse(Convert.ToString(cells[row, 9].Value));
                    }
                    bi.Assign = Convert.ToString(cells[row, 10].Value).Trim();
                    bs.Items.Add(bi);

                    row++;
                    tmp = Convert.ToString(cells[row, 0].Value);
                }
                row++;
                bs.Kredit = decimal.Parse(Convert.ToString(cells[row, 8].Value).Replace(".", ","));
                bs.Debet = decimal.Parse(Convert.ToString(cells[row, 9].Value).Replace(".", ","));
            }

            return bs;
        }
    }
}
