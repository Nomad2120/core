using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Swagger
{
    public class SwaggerIgnoreOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation?.Parameters == null)
            {
                return;
            }

            var ignoreParameters = context.MethodInfo.GetParameters().Where(p => p.GetCustomAttribute<SwaggerIgnoreAttribute>() != null);

            foreach (var ignoreParameter in ignoreParameters)
            {
                var parameterToIgnore = operation.Parameters.SingleOrDefault(x => string.Equals(x.Name, ignoreParameter.Name, StringComparison.OrdinalIgnoreCase));

                if (parameterToIgnore != null)
                {
                    operation.Parameters.Remove(parameterToIgnore);
                }
            }
        }
    }
}
