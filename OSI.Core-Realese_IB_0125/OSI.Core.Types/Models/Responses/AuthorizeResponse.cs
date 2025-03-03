using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class AuthorizeResponse
    {
        public int UserId { get; set; }
        public string Token { get; set; }
    }
}
