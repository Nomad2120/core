using OSI.Core.Models.AccountReports;
using System.Collections.Generic;

namespace OSI.Core.Logic.BankStatementParsing
{
    public interface IStatementParser
    {
        bool CheckFileFormat(byte[] data);
        BankStatement ParseData(byte[] data);
    }
}