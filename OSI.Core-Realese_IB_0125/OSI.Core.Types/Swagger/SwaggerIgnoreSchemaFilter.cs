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
    public class SwaggerIgnoreSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema?.Properties == null)
            {
                return;
            }

            var ignoreProperties = context.Type.GetProperties().Where(p => p.GetCustomAttribute<SwaggerIgnoreAttribute>() != null);

            foreach (var ignoreProperty in ignoreProperties)
            {
                var propertyToIgnore = schema.Properties.Keys.SingleOrDefault(x => string.Equals(x, ignoreProperty.Name, StringComparison.OrdinalIgnoreCase));

                if (propertyToIgnore != null)
                {
                    schema.Properties.Remove(propertyToIgnore);
                }
            }
        }
    }
}
