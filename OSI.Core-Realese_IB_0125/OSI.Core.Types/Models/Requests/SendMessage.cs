using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests.Telegram
{
    public class SendMessage
    {
        public string Phone { get; set; }

        public string Message { get; set; }
    }
}
