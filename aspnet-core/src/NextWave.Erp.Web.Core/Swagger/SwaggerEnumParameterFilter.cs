using System;
using System.Collections;
using System.Linq;
using Abp.Collections.Extensions;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NextWave.Erp.Web.Swagger;

public class SwaggerEnumParameterFilter : IParameterFilter
{
    public void Apply(IOpenApiParameter parameter, ParameterFilterContext context)
    {
        var type = Nullable.GetUnderlyingType(context.ApiParameterDescription.Type) ?? context.ApiParameterDescription.Type;
        if (type.IsEnum)
        {
            AddEnumParamSpec(parameter, type, context);
            if (parameter is OpenApiParameter openApiParameter)
            {
                openApiParameter.Required = type == context.ApiParameterDescription.Type;
            }
        }
        else if (type.IsArray || (type.IsGenericType && type.GetInterfaces().Contains(typeof(IEnumerable))))
        {
            var itemType = type.GetElementType() ?? type.GenericTypeArguments.First();
            AddEnumSpec(itemType, context);
        }
    }

    private static void AddEnumSpec(Type type, ParameterFilterContext context)
    {
        var schema = context.SchemaRepository.Schemas.GetOrAdd($"{type.Name}", () =>
            context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository)
        );

        if (!type.IsEnum)
        {
            return;
        }

        schema.AddEnumNamesExtension(type);
    }

    private static void AddEnumParamSpec(IOpenApiParameter parameter, Type type, ParameterFilterContext context)
    {
        var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
        if (parameter is not OpenApiParameter openApiParameter)
        {
            return;
        }

        openApiParameter.Schema = schema;
        schema.AddEnumNamesExtension(type);
    }
}
