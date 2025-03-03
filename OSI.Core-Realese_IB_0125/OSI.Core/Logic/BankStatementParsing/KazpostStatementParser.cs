using OSI.Core.Models.AccountReports;
using OSI.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSI.Core.Logic.BankStatementParsing
{
    public class KazpostStatementParser : IStatementParser
    {
        public bool CheckFileFormat(byte[] data)
        {
            if (data.Length == 0)
                return false;

            return (data[0] == 49 && data[1] == 67 && data[2] == 67);
        }

        public (decimal Begin, decimal Debet, decimal Kredit, decimal End) GetTotalAmounts(List<string> lines)
        {
            // ведем подсчет сумм поступления и списания по дням, т.к. халык дает отчет по дням
            int i = 0;
            string line = "";
            decimal begin = 0;
            decimal debet = 0;
            decimal kredit = 0;
            decimal end = 0;
            bool beginPassed = false;
            while (i < lines.Count)
            {
                line = lines[i++];
                if (line.StartsWith("НачальныйОстаток"))
                {
                    if (!beginPassed)
                    {
                        begin = decimal.Parse(line.Split('=')[1].Replace(".", ","));
                        beginPassed = true;
                    }
                }
                else if (line.StartsWith("СуммаПриход"))
                {
                    debet += decimal.Parse(line.Split('=')[1].Replace(".", ","));
                }
                else if (line.StartsWith("СуммаРасход"))
                {
                    kredit += decimal.Parse(line.Split('=')[1].Replace(".", ","));
                }
                else if (line.StartsWith("КонечныйОстаток"))
                {
                    end = decimal.Parse(line.Split('=')[1].Replace(".", ","));
                }
            }

            return (begin, debet, kredit, end);
        }

        public DateTime GetPeriodBegin(List<string> lines)
        {
            if (lines.Count < 2 || !lines[1].StartsWith("ДАТАНАЧАЛА"))
                throw new Exception("Ошибка при проверке даты начала выписки: Неверный формат файла");

            if (!DateTime.TryParse(lines[1].Split('=')[1], out DateTime dt))
                throw new Exception("Ошибка при проверке даты начала выписки: Неверный формат даты");

            return dt;
        }

        public DateTime GetPeriodEnd(List<string> lines)
        {
            if (lines.Count < 3 || !lines[2].StartsWith("ДАТАКОНЦА"))
                throw new Exception("Ошибка при проверке даты конца выписки: Неверный формат файла");

            if (!DateTime.TryParse(lines[2].Split('=')[1], out DateTime dt))
                throw new Exception("Ошибка при проверке даты конца выписки: Неверный формат даты");

            return dt;
        }

        public string GetAccount(List<string> lines)
        {
            if (lines.Count < 4 || !lines[3].StartsWith("РАСЧСЧЕТ"))
                throw new Exception("Ошибка при проверке расчетного счета: Неверный формат файла");

            string tmp = lines[3].Split('=')[1];
            return tmp;
        }

        public List<BankStatementItem> GetStatementItems(List<string> lines)
        {
            int i = 0;
            string line = "";
            bool itemAssingBegin = false;
            List<BankStatementItem> items = new List<BankStatementItem>();
            BankStatementItem item = null;
            while (i < lines.Count)
            {
                line = lines[i++];
                if (itemAssingBegin && !line.StartsWith("КОНЕЦДОКУМЕНТА"))
                {
                    item.Assign += " " + line;
                }
                else if (line.StartsWith("СЕКЦИЯДОКУМЕНТ"))
                {
                    item = new BankStatementItem();
                }
                else if (line.StartsWith("ДатаДокумента"))
                {
                    if (!DateTime.TryParse(line.Split('=')[1], out DateTime dt))
                        throw new Exception("Ошибка при проверке даты очередного документа: Неверный формат даты");
                    item.Dt = dt;
                }
                else if (line.StartsWith("ПолучательНаименование"))
                {
                    item.Receiver = line.Split('=')[1];
                }
                else if (line.StartsWith("ПолучательБИН_ИИН"))
                {
                    item.ReceiverBin = line.Split('=')[1];
                }
                else if (line.StartsWith("ПлательщикНаименование"))
                {
                    item.Sender = line.Split('=')[1];
                }
                else if (line.StartsWith("ПлательщикБИН_ИИН"))
                {
                    item.SenderBin = line.Split('=')[1];
                }
                else if (line.StartsWith("СуммаРасход") || line.StartsWith("СуммаПриход"))
                {
                    item.Amount = decimal.Parse(line.Split('=')[1].Replace(".", ","));
                    item.OperationTypeCode = line.StartsWith("СуммаРасход") ? OperationTypeCodes.DEBET : OperationTypeCodes.KREDIT;
                }
                else if (line.StartsWith("НазначениеПлатежа"))
                {
                    item.Assign = line.Split('=')[1];
                    itemAssingBegin = true;
                } 
                else if (line.StartsWith("КОНЕЦДОКУМЕНТА"))
                {
                    itemAssingBegin = false;
                    item.Assign = item.Assign.Replace("\r\n", " ").Replace("  ", " ");
                    if (item?.Amount != 0)
                    {
                        items.Add(item);
                    }
                }
            }

            return items;
        }

        public BankStatement ParseData(byte[] data)
        {
            string text = Encoding.UTF8.GetString(data);

            if (text.IndexOf("ДАТАНАЧАЛА") == -1)
            {
                text = Encoding.GetEncoding(1251).GetString(data);
                if (text.IndexOf("ДАТАНАЧАЛА") == -1)
                {
                    throw new Exception("Не удалось определить кодировку файла");
                }
            }

            List<string> lines = text.Replace("\r", "").Split('\n').Select(s => s.TrimStart(' ')).ToList();
            BankStatement bs = new BankStatement();
            bs.Account = GetAccount(lines);
            bs.PeriodStart = GetPeriodBegin(lines);
            bs.PeriodEnd = GetPeriodEnd(lines);
            var totals = GetTotalAmounts(lines);
            bs.Begin = totals.Begin;
            bs.Debet = totals.Debet;
            bs.Kredit = totals.Kredit;
            bs.End = totals.End;
            bs.Items = GetStatementItems(lines);
            return bs;
        }
    }
}
