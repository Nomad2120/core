using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Models.Requests.Telegram
{
    public class CallMeBackNotificationRequest
    {
        public string Phone { get; set; }

        public string Name { get; set; }

        public bool AfterInactivity { get; set; }
    }
}
