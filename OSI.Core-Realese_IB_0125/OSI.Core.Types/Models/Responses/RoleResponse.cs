using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.Core.Models.Responses
{
    public class RoleResponse
    {
        public string NameRu { get; set; }

        public string NameKz { get; set; }

        public string Role { get; set; }
    }
}
