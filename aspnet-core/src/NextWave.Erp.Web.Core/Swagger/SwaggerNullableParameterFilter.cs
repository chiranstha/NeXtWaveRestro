using System;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NextWave.Erp.Web.Swagger;

public class SwaggerNullableParameterFilter : IParameterFilter
{
    public void Apply(IOpenApiParameter parameter, ParameterFilterContext context)
    {
        if (parameter.Schema != null &&
            (context.ApiParameterDescription.Type.IsNullable() || !context.ApiParameterDescription.Type.IsValueType))
        {
            parameter.Schema.MakeNullable();
        }
    }
}

public static class TypeExtensions
{
    public static bool IsNullable(this Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }
}
