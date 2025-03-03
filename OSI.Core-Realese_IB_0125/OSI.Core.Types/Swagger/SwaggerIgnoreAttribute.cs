using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Swagger
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public class SwaggerIgnoreAttribute : Attribute
    {
    }
}
