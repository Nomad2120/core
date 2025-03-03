using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Helpers
{
    public static class ExceptionHelpers
    {
        public static string GetFullInfo(this Exception ex, bool includeInnerExceptions = true, bool includeStackTrace = false)
        {
            return GetFullInfoInternal(ex, includeInnerExceptions, includeStackTrace);
        }

        private static string GetFullInfoInternal(this Exception ex, bool includeInnerExceptions = true, bool includeStackTrace = false, int level = 0)
        {
            string indent = "";
            for (int i = 0; i < level; i++)
            {
                indent += "    ";
            }
            string exceptionInfo = indent + ex.Message;
            if (includeStackTrace)
            {
                exceptionInfo +=
                    Environment.NewLine + indent + "Stack Trace:" +
                    Environment.NewLine + indent + ex.StackTrace;
            }
            if (ex.InnerException != null && includeInnerExceptions)
            {
                exceptionInfo +=
                    Environment.NewLine + indent + "Inner Exception:" +
                    Environment.NewLine + GetFullInfoInternal(ex.InnerException, includeInnerExceptions, includeStackTrace, level + 1);
            }
            return exceptionInfo;
        }
    }
}
