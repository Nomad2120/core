using ESoft.CommonLibrary;
using OSI.Core.Models.Db;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    /// <summary>
    /// Информация об абоненте
    /// </summary>
    public class AbonentInfoResponse
    {
        public AbonentInfoResponse(string abonentNum, Abonent abonent, EndSaldoResponse endSaldoResponse)
        {
            AbonentNum = abonentNum;
            OsiName = abonent.OsiName;
            Address = abonent.Address;
            InvoiceNum = abonent.InvoiceNum;
            CreationDate = DateTime.ParseExact(InvoiceNum[..6], "yyMMdd", DateTimeFormatInfo.InvariantInfo);
            Period = CreationDate.AddDays(1 - CreationDate.Day);
            ExpirationDate = Period.AddMonths(1).AddDays(-1);
            Services = endSaldoResponse?.Services?.Select(s => new PaymentServiceInfo(s)).ToList();
        }

        /// <summary>
        /// Номер абонента
        /// </summary>
        public string AbonentNum { get; set; }

        /// <summary>
        /// Наименование ОСИ
        /// </summary>
        public string OsiName { get; set; }

        /// <summary>
        /// Адрес абонента
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Номер инвойса
        /// </summary>
        public string InvoiceNum { get; set; }

        /// <summary>
        /// Период инвойса
        /// </summary>
        public DateTime Period { get; set; }

        /// <summary>
        /// Дата формирования инвойса
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Дата, до которой необходимо оплатить инвойс
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Список услуг
        /// </summary>
        public List<PaymentServiceInfo> Services { get; set; }
    }
}
