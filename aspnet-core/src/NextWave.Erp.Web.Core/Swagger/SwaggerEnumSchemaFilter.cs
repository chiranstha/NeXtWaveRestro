using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NextWave.Erp.Web.Swagger;

public class SwaggerEnumSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var type = context.Type;
        if (!type.IsEnum || schema.Extensions?.ContainsKey("x-enumNames") == true)
        {
            return;
        }

        schema.AddEnumNamesExtension(type);
    }
}
