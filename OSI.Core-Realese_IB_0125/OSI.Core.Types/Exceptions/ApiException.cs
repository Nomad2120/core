using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Exceptions
{
    public class ApiException : Exception
    {
        public int Code { get; }

        public ApiException(string message) : this(-1, message)
        {
        }

        public ApiException(int code, string message) : base(message)
        {
            Code = code;
        }

        public ApiException(int code, string message, Exception innerException) : base(message, innerException)
        {
            Code = code;
        }
    }
}
