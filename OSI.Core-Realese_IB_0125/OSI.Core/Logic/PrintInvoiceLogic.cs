using Aspose.Cells;
using Microsoft.EntityFrameworkCore;
using OSI.Core.Comparer;
using OSI.Core.Models.Db;
using OSI.Core.Models.Reports;
using OSI.Core.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Logic
{
    public static class PrintInvoiceLogic
    {
        public static async Task<PrintInvoicesResult> GetInvoicesByListOfAbonents(bool isStraightOrder,
                                                                             bool isOnlyResidents,
                                                                             List<OSVAbonent> osvAbonents,
                                                                             List<AbonentAccural> accurals,
                                                                             string osiName,
                                                                             string osiAddress,
                                                                             string period,
                                                                             Func<string, string> getQR)
        {
            string fileContent = InvoiceHeader;

            // OSI-258 Сортировка абонентов
            int invoicesOnPage = 3;

            // isGenerallyFirst - первый во всем документе
            string GetContent(OSVAbonent osvAbonent, bool lastOnPage, bool isGenerallyFirst, Osi osi)
            {
                var abonentAccurals = accurals.Where(a => a.AbonentId == osvAbonent.AbonentId).ToList(); // OSI-230, 21-02-2023, shuma
                var content = InvoiceBody(osvAbonent,
                                          abonentAccurals,
                                          osi,
                                          osiName,
                                          osiAddress,
                                          isFirstInvoice: isGenerallyFirst,
                                          lastOnPage,
                                          invoicesOnPage,
                                          period,
                                          s => getQR(s));
                return content;
            }

            // фильтруем резидент/нерезидент
            var filteredList = isOnlyResidents ? GetOnlyResidents(osvAbonents) : osvAbonents;

            // сортировка
            var arrangedAbonents = isStraightOrder ? GetStraightPrintOrder(filteredList, invoicesOnPage) : GetThroughPrintOrder(filteredList, invoicesOnPage);

            int count = 0;
            using var db = OSIBillingDbContext.DbContext;
            Osi osi = null;
            foreach (var abonent in arrangedAbonents)
            {
                count++;
                if (osi == null)
                {
                    var ab = await db.Abonents.FirstOrDefaultAsync(a => a.Id == abonent.Key.AbonentId);
                    if (ab != null)
                    {
                        osi = await db.Osies
                            .Include(a => a.UnionType)
                            .Include(a => a.OsiServiceCompanies)
                            .ThenInclude(q => q.ServiceCompany)
                            .FirstOrDefaultAsync(a => a.Id == ab.OsiId);
                    }
                }

                fileContent += GetContent(abonent.Key, abonent.Value, isGenerallyFirst: count == 1, osi);
            }

            fileContent += InvoiceFooter;

            return new PrintInvoicesResult
            {
                Count = count,
                Data = fileContent
            };
        }

        public static string InvoiceHeader =>
            @"<!DOCTYPE html>
                <html><head>
                <meta charset=""utf-8""/>
                <style type=""text/css"">
                    td.table-cell {
                        border: 1pt solid black;
                    }
                    tr.table-header {
                        /*height:16pt;*/ vertical-align:middle; text-align:center; font-size:6pt;
                    }
                    tr.uptable-row {
                        height:12pt; vertical-align:middle; text-align:right; font-size:6pt;
                    }
                    td.uptable-sep {
                        width:3%; border-bottom: 0pt; border-top: 0pt
                    }
                    .font-bold {
                        font-weight:Bold
                    }       

                    hr {
                        border:none;
                        border-top:1px dotted #000;
                        margin-left: -5mm;
                        margin-right: -5mm;
                        margin-top: 15px;
                        margin-bottom: 15px;
                    }

                    td.qr {
                        font-size: 8px; 
                        font-weight: bold; 
                        font-family: Arial, Helvetica, sans-serif;
                    }
                </style>
                </head>
                <body id=""body"">";

        public static string InvoiceFooter =>
                //@$"<script type=""text/javascript"">
                //    var d = document.createElement(""div"");
                //    d.style = ""position: absolute; top: -1000mm; left : -1000mm; height : 1000mm; width : 1000mm"";
                //    document.getElementById(""body"").appendChild(d);
                //    var px_per_mm = d.scrollHeight / 1000;
                //    d.remove();
                //    var page_height = Math.floor(290 / {invoicesOnPage});
                //    var pages = document.getElementsByClassName(""page-container"");
                //    for (var i = 0; i<pages.length; i++) {{
                //        var p = pages[i].children[0];
                //        var mm_height = Math.round(p.clientHeight / px_per_mm) + 20;
                //        mm_height = page_height * (Math.floor(mm_height / page_height) + (mm_height % page_height > 0 ? 1 : 0));
                //        pages[i].style.height = mm_height + ""mm"";
                //    }}
                //</script>
                //</body>
                //</html>";
                @"</body>
                </html>";

        public static string InvoiceBody(OSVAbonent osvAbonent,
                                         List<AbonentAccural> accurals,
                                         Osi osi,
                                         string osiName,
                                         string osiAddress,
                                         bool isFirstInvoice,
                                         bool isLastOnPage,
                                         int invoicesOnPage,
                                         string period,
                                         Func<string, string> getQR)
        {
            string result =
                @$"<div style=""width:210mm; height:calc({(isLastOnPage ? "285" : "290")}mm/{invoicesOnPage}); margin-top:{(isFirstInvoice ? "-6" : "0")}pt; margin-left:-6pt; page-break-after: {(isLastOnPage ? "always" : "none")}"" class=""page-container"">";

            //result += @"<div style=""position:absolute; margin:10mm;"">";
            result += @$"<div style=""position:absolute; padding-left:10mm; padding-right:10mm; padding-top:10mm; padding-bottom:0mm"">";

            string address = osiAddress + ", " + osvAbonent.Flat;
            string account = string.IsNullOrEmpty(osvAbonent.ErcAccount) ? osvAbonent.AbonentId.ToString() : osvAbonent.ErcAccount;

            result += @$"<table border=0 cellpadding=0 cellspacing=0
                style=""width:100%; font-family:Segoe UI; border-collapse:collapse; text-align:left; margin-bottom:5px"">
                <tr>
                    <td colspan=""2"" style=""font-family:Segoe UI; font-size:12pt; font-weight:Bold; margin-top: -5pt;"">
                        Дербес шот/ Лицевой счет № {account}</td>
                    <td style=""width:20%; font-size:12pt; font-weight:Bold; text-align: center;"">{period}</td>
                    <td rowspan=""2"" style=""width:30%"">
                        <table border=0 cellpadding=0 cellspacing=0 style=""float: right; "">
                        <tr>
                            <td colspan=""2"" style=""align-items: center; vertical-align: middle;"">
                                <img style=""width:20px; height:20px; padding-left: 2px; padding-top: 2px; float: left;"" 
                                   src=""data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAACXBIWXMAAA7EAAAOxAGVKw4bAAAFCElEQVR42qVWiW8UdRTev8J4H0FBClIELEdLRYS2tkgVY4mioCKJ0VYqkIgcYlAhElABg0QwggU5tQWj4FEtN1FuW3vQAyiHFHZmtrOzO/fne292t7toTBMneZmd6W/e8b3ve6+h+LaNiJQVQB0/DMroAeie+RL48jwPvbmS55y2FqjjhkAteAjqI9nQ57wC37YQ0h4bCe3x0Yi8MAkaBYq+Mwe+68BzbLlnmO/DJ4e+0/POs4NzbkcrtCkliEwtRWTKBAkU3/4lQsqDd0Kf9zp8y4QXj+P/XD6bacK90AG1aDiMFe8iFH7gFuhvlQfl6t0wa7YiXvUZ4pvW9djmz8nWw+uOwD59HLENa4N3qTPrEd+4FtbhfeLHvXKJ4BoK46P3EWLcokvmSfnGJ8ugDLlLINNKHw5sYj60whyEs2+H094KY9UHCA+8OXifPFOaL5Aouf3htDbB01RokwsRW78aIc7Ki0YlMkPVPaNMMnXaz9LhFriXL8I+8RuUUffD7TyH2OpliDxTLNW659qkue6lC3CaG6BSAOtwnUDlRTSBPSTYJZgQnU8BKqbBOdsElZqvFuRAXzgLTlM9wjn3BgE+XoLItCfJeTtUqpSx7p41Ay5Vp+ZmwT56gBz6gghfIf7hESuCABXQy6fCrj8FZURfKMP6CG3thtNQOAA1jwN0UwBOQsnLgjL8PkReLguSSgRI+uR7UIHrpiDSuYKONqJsIbSnxiG6dCGclj+pgj5BBSuXEhWfEHi0Z0ugTRpL31XAPU8VjepPAfYj3WdGgOii2Yi8OAneX5d7ekBOrYO/ZvaAHPM3HMS9ekXwto8dhZo/MIDoxgApiBZWgmmrjh1MahwsihQjhYcH3SaVGcsWCfYeNZDPxXdUwTl1jOC8B2HSlHWwDuk+gwoSDXGJYvaBWthH9sEmNvQYPR8idpAQWbH2yd/lPJ9zO8/Du9YF8/tqOccU9dObnM4imzIxq7fC/HYHzF3be2w3PX+zRajJdIzv3ASeYSbd3YsXUko2v/4KHkGW7jMTovkzCaJboRXTfCrJhTaBrHiUCI3xdYiaBrEonHVTIC56Z9X9KFlbtXug5qWxyHX+vckpoREU0kQSkR8zKHtdsjJWLEbk+VL4RpT+1klishD7Yo2o2LveFQzCf0CURtOU0ChzFpG+4A3KvA2R5yYKY4ylC6BS9hyAKWr+sJt6cBX2meM9Qy/hPBAaZfXfQptOzycRHnwHKboBDonO2v+zZMpCM/fUEEWPQJ/7GqJvk+qbG4MgGUJLROMeBEKj2f50AWX4KA3C+SI0ZWQ/GRk8Iqxf9iJes4200U9+WyQubXKRUNU6VId0nyEeZm74WgKiikBoXVdkcsogIxryR0mhGSQ0nqZq0QgopJX4lg3UZEUqU/MGyMjmzNkv9zKkjOxLpc2Wl7F1qxAecrfMcnX80ODONiYbvJhEaB++R+M8T3BnSkYXVMo8UscMElEKMZTrMvJja5bTwqE5rxM0UhbtUGt/LczvqmHt2UVWE9jeXSSkGnjUWKexHta+n1JNdM6cIN3slG+cxj8CwdKo4f0QW7kksTLnltM8UQmq6/B7ux55N9/wj4FnGAKX29IIlbRjLF9MS5/KZSEx5RiWYOm7iWXuZlpy6SdonRQpPzutzVBZmLzheE/QfykmESHkECWZKfqbr0KvnE779tMMqfemkuQeZh9RQkOnBRSvWifv/wZ1hY+U01JlggAAAABJRU5ErkJggg=="" />
                                <span style=""font-size: 20px; font-weight: bold; font-family: Arial, Helvetica, sans-serif; padding-left: 3px;"">Kaspi QR</span>
                            </td>
                        </tr>
                        <tr>
                            <td style=""width:60px;"" rowspan=""6"">
                                <img style=""width:60px; height:60px;"" src=""data:image/png;base64,{getQR(account)}"">
                            </td>
                        </tr>
                        <tr><td class=""qr"">Сканерлеп, тубiртектi толенiз</td></tr>
                        <tr><td class=""qr"">Комиссия - 0 тг</td></tr>
                        <tr><td><div style=""background-color: #F04635; width:25px; height:3px; margin-top: 3px;""></div></td></tr>
                        <tr><td class=""qr"">Сканируйте и оплачивайте квитанцию</td></tr>
                        <tr><td class=""qr"">Комиссия - 0 тг</td></tr>
                    </table>
                </td>
                </tr>
                <tr style=""vertical-align:middle;"">
                    <td style=""width:20%"">
                        <div style=""margin-right: 15px; margin-top: 5px"">
                            <img style=""width:100%; height:auto; vertical-align: top""
                                src=""data:image/png;base64,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"">
                        </div>
                    </td>
                    <td colspan=2 style=""font-family:Segoe UI; font-size:10pt; text-align: center;"">{osiName}<br>
                        {address}
                    </td>
                </tr>
            </table>";

            // таблица
            result += @"<table border=0 cellpadding=1 cellspacing=0 style=""width:100%; font-family:Segoe UI; font-size:6pt; text-align:center; vertical-align:middle; border-collapse:collapse"">";

            result += @"<tr>
                        <td class=""table-cell"" style=""width:30%"">Қызметтердің аталуы /<br>Наименование услуг</td>
                        <td class=""table-cell"">Шаршы /<br>Площадь</td>
                        <td class=""table-cell"">Тариф /<br>Тариф</td>
                        <td class=""table-cell"">Бастау үшін қарыз /<br>Долг на начало</td>
                        <td class=""table-cell"">Есептелдi /<br>Начислено</td>
                        <td class=""table-cell"">Қайта есептеу /<br>Перерасчет</td>
                        <td class=""table-cell"">Өсімпұл /<br>Пеня</td>
                        <td class=""table-cell"">Төлемген /<br>Оплачено</td>
                        <td class=""table-cell font-bold"">Төлемге /<br>К оплате</td>
                        </tr>";

            decimal totalSum = 0;
            foreach (OSVSaldo saldo in osvAbonent.Services)
            {
                result += $@"<tr>
                        <td class=""table-cell"" style=""text-align:left;"">{saldo.ServiceName}</td>
                        <td class=""table-cell""></td>";

                // OSI-388 тариф по кап.ремонту
                if (saldo.ServiceName == "Взнос на капитальный ремонт здания" && (accurals?.Any(a => a.GroupId == 2) ?? false))
                {
                    // должна быть только одна услуга
                    var bigRepairAccural = accurals.FirstOrDefault(a => a.GroupId == 2);
                    result += $@"<td class=""table-cell"">{bigRepairAccural?.Tarif}</td>";
                }
                else
                {
                    result += @"<td class=""table-cell""></td>";
                }
                result += $@"<td class=""table-cell"">{saldo.Begin.ToString("0.00")}</td>
                        <td class=""table-cell"">{saldo.SumOfAccurals.ToString("0.00")}</td>
                        <td class=""table-cell"">{(saldo.SumOfFixes == 0 ? "" : saldo.SumOfFixes.ToString("0.00"))}</td>
                        <td class=""table-cell"">{(saldo.SumOfFines == 0 ? "" : saldo.SumOfFines.ToString("0.00"))}</td>
                        <td class=""table-cell"">{saldo.Kredit.ToString("0.00")}</td>
                        <td class=""table-cell font-bold"" style=""text-align:right"">{saldo.End.ToString("0.00")}</td>
                        </tr>";
                totalSum += saldo.End <= 0 ? 0 : saldo.End; // OSI-298

                // OSI-230, 21-02-2023, shuma
                if (saldo.ServiceName == "Взнос на содержание общего имущества" && (accurals?.Any(a => a.GroupId == 1) ?? false))
                {
                    var serviceGroupings = accurals
                        .Where(a => a.GroupId == 1)
                        .GroupBy(a => new { a.AbonentId, a.Square, a.EffectiveSquare, a.ServiceId, a.ServiceName/*, a.Tarif, a.AccuralMethodCode*/ });

                    decimal sumOfFixes = 0;
                    foreach (var sg in serviceGroupings)
                    {
                        // OSI-320, если в начислениях попадаются корректировки, где не может быть указан AccuralMethodCode
                        // ищем в группировке такую услугу где указан и тариф и метод начисления
                        AbonentAccural abonentAccuralWithTarifAndAccuralMethod = sg.FirstOrDefault(a => a.Tarif != null && !string.IsNullOrEmpty(a.AccuralMethodCode));
                        //if (abonentAccuralWithTarifAndAccuralMethod == null)
                        //{
                        //    throw new Exception($"Abonent ID = {sg.Key.AbonentId}, Service ID = {sg.Key.ServiceId}: Не указан тариф и метод начисления");
                        //}

                        string serviceName = sg.Key.ServiceName == "Услуги OSI billing" ? "ведение учета" : sg.Key.ServiceName[0].ToString().ToLower() + sg.Key.ServiceName.Substring(1);

                        result += $@"<tr><td class=""table-cell"" style=""text-align:left; padding-left: 50px;"">в т.ч. {serviceName}</td>";
                        if (abonentAccuralWithTarifAndAccuralMethod?.AccuralMethodCode == "TARIF_1KVM")
                        {
                            result += $@"<td class=""table-cell"">{sg.Key.Square}</td>
                                         <td class=""table-cell"">{abonentAccuralWithTarifAndAccuralMethod.Tarif}</td>";
                        }
                        else if (abonentAccuralWithTarifAndAccuralMethod?.AccuralMethodCode == "FIX_SUM_FLAT")
                        {
                            result += $@"<td class=""table-cell""></td>
                                         <td class=""table-cell"">{abonentAccuralWithTarifAndAccuralMethod.Tarif}</td>";
                        }
                        else if (abonentAccuralWithTarifAndAccuralMethod?.AccuralMethodCode == "TARIF_1KVM_EFF")
                        {
                            result += $@"<td class=""table-cell"">{sg.Key.EffectiveSquare}</td>
                                         <td class=""table-cell"">{abonentAccuralWithTarifAndAccuralMethod.Tarif}</td>";
                        }
                        else
                        {
                            result += $@"<td class=""table-cell""></td>
                                         <td class=""table-cell""></td>";
                        }
                        sumOfFixes = sg.Sum(s => s.SumOfFixes);
                        result += $@"<td class=""table-cell""></td>
                                     <td class=""table-cell"">{sg.Sum(s => s.DebetWithoutFixes).ToString("0.00")}</td>
                                     <td class=""table-cell"">{(sumOfFixes == 0 ? "" : sumOfFixes.ToString("0.00"))}</td>
                                     <td class=""table-cell""></td>
                                     <td class=""table-cell""></td>
                                     <td class=""table-cell""></td>
                                     </tr>";
                    }
                }
            }

            result += $@"<tr>
                        <td colspan=""7"" style=""border: 0pt; text-align:center; vertical-align:top""></td>
                        <td class=""table-cell font-bold"" style=""font-size:6pt"">Барлығы<br>Итого</td>
                        <td class=""table-cell font-bold"" style=""text-align:right; font-size:6pt"">{totalSum.ToString("0.00")}</td>
                        </tr>";

            result += "</table>";

            // информация
            result += @"<table border=0 cellpadding=1 cellspacing=0 style=""width:100%; font-family:Segoe UI; font-size:6pt; text-align:justify; border-collapse:collapse"">";
            result += "<tr><td>";
            result += @"&nbsp;&nbsp;&nbsp;&nbsp;Қызметтерді төлеу үшін сізге Каспий банк қосымшасында Төлемдер бөлімін таңдап, eOSI қызмет провайдерін тауып, осы түбіртекте көрсетілген жеке шот нөмірін енгізіп, төлемді жүзеге асыру қажет. 
                        OSI/PT қызметі, белгіленген тарифтер, қарыздар және т.б. туралы толық және сенімді ақпаратты алу үшін <u>www.eosi.kz</u> сайтында тіркелуді ұсынамыз. Тіркелу үшін орнатылған Telegram мессенджері қажет.
                        <br>&nbsp;&nbsp;&nbsp;&nbsp;Для оплаты услуг Вам необходимо в приложении Каспий банк выбрать раздел Платежи, найти поставщика услуг eOSI, ввести номер своего лицевого счета, указанный в данной квитанции, произвести оплату. 
                        Для получения полной и достоверной информации о деятельности Вашего ОСИ/ПТ, установленных тарифах, задолженностях и прочем, предлагаем Вам зарегистрироваться на сайте <u>www.eosi.kz</u>. Для регистрации необходим установленный мессенджер Telegram.";
            result += "</td></tr>";
            result += "</table>";

            // OSI-643, выводим все организации, игнориуя галочку ShowPhones, и указываем номер председателя там, где не указан номер представителя ОСИ
            string phones = "Председатель: " + osi.Phone;
            if (osi?.OsiServiceCompanies.Any() ?? false)
            {
                OsiServiceCompany spokesman = osi.OsiServiceCompanies.FirstOrDefault(a => a.ServiceCompanyCode == "SPOKESMAN");
                if (spokesman != null)
                {
                    phones = "Представитель: " + (string.IsNullOrEmpty(spokesman.Phones) ? osi.Phone : spokesman.Phones);
                }

                foreach (OsiServiceCompany c in osi.OsiServiceCompanies.Where(a => a.ServiceCompanyCode != "SPOKESMAN"))
                {                    
                    phones += "; " + c.ServiceCompanyNameRu + " " + c.Phones;
                }
            }
            result += $@"<p style=""font-family: Segoe UI; font-size: 6pt"">
                            Телефоны {osi.UnionTypeRu}: {phones.TrimEnd(new char[] { ' ', ';' })}
                        </p>";

            //result += $@"</div><br>          
            //          <div style=""/*border: 0.5mm solid black; */position:absolute; margin-top: 280.9mm; margin-left: 3mm; width: 11mm;""></div>
            //          <div style=""/*border: 0.5mm solid black; */position:absolute; margin-top: 271.9mm; margin-left: 3mm; width: 11mm;""></div>
            //          <div style=""/*border: 0.5mm solid black; */position:absolute; margin-top: 263.9mm; margin-left: 3mm; width: 11mm;""></div>      
            //          <div style=""/*border: 0.5mm solid black; */position:absolute; margin-top: 280.5mm; margin-left: 37.5mm; height: 11mm;""></div>
            //          <div style=""/*border: 0.5mm solid black; */position:absolute; margin-top: 280.5mm; margin-left: 45.9mm; height: 11mm;""></div>
            //          <div style=""/*border: 0.5mm solid black; */position:absolute; margin-top: 280.5mm; margin-left: 55.1mm; height: 11mm;""></div>    
            //          </div>";

            result += $@"</div><br></div>";

            return result;
        }

        public static List<OSVAbonent> GetNaturalComparerOrderList(List<OSVAbonent> osvAbonents)
        {
            return osvAbonents.OrderBy(oa => oa.Flat, NaturalComparer.Instance).ToList();
        }

        public static List<OSVAbonent> GetOnlyResidents(List<OSVAbonent> osvAbonents)
        {
            return osvAbonents.Where(o => o.AreaTypeCode != Models.Enums.AreaTypeCodes.NON_RESIDENTIAL).ToList();
        }

        public static Dictionary<OSVAbonent, bool> GetStraightPrintOrder(List<OSVAbonent> osvAbonents, int invoicesOnPage)
        {
            var abonents = GetNaturalComparerOrderList(osvAbonents);
            Dictionary<OSVAbonent, bool> result = new Dictionary<OSVAbonent, bool>();
            int length = abonents.Count;
            for (int i = 0; i < length; i++)
            {
                result.Add(abonents[i], ((i + 1) % invoicesOnPage == 0) || i + 1 == length);
            }
            return result;
        }

        public static Dictionary<OSVAbonent, bool> GetThroughPrintOrder(List<OSVAbonent> osvAbonents, int invoicesOnPage)
        {
            var abonents = GetNaturalComparerOrderList(osvAbonents);

            Dictionary<OSVAbonent, bool> result = new Dictionary<OSVAbonent, bool>();

            int length = abonents.Count;

            // получаем кол-во листов и шаг для печати всех квитанций с учетом кол-во квитанций на странице
            int incStep = length / invoicesOnPage + (length % invoicesOnPage > 0 ? 1 : 0); 

            // бежим по кол-ву страниц
            for (int i = 0; i < incStep; i++)
            {
                // первый абонент на странице; его признак lastOnPage зависит от кол-ва инвойсов на страницу
                result.Add(abonents[i], invoicesOnPage == 1);

                // проверяем вместимость остальных на страницу
                for (int j = 1; j < invoicesOnPage + 1; j++)
                {
                    if (i + incStep * j < length)
                    {
                        bool lastOnPage = (i + incStep * (j + 1) >= length) || (j == invoicesOnPage);
                        result.Add(abonents[i + incStep * j], lastOnPage);                        
                    }
                }
            }

            return result;
        }
    }
}
