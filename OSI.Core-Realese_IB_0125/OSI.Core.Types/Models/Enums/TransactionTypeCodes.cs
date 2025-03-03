using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Models.Enums
{
    public enum TransactionTypeCodes
    {
        SALDO,   // ввод начального сальдо
        ACC,     // начисления
        PAY,     // оплаты
        FIX,     // корректировки
        FINE,    // пеня
    }
}
