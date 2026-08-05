using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace NextWave.Erp.Web.Swagger;

internal static class OpenApiSchemaExtensions
{
    private const string EnumNamesExtension = "x-enumNames";

    public static void AddEnumNamesExtension(this IOpenApiSchema schema, Type type)
    {
        var extensions = schema.Extensions;
        if (extensions == null)
        {
            if (schema is not OpenApiSchema openApiSchema)
            {
                return;
            }

            openApiSchema.Extensions = new Dictionary<string, IOpenApiExtension>();
            extensions = openApiSchema.Extensions;
        }

        var enumNames = new JsonArray(Enum.GetNames(type)
            .Select(name => JsonValue.Create(name))
            .ToArray<JsonNode>());

        extensions[EnumNamesExtension] = new JsonNodeExtension(enumNames);
    }

    public static void MakeNullable(this IOpenApiSchema schema)
    {
        if (schema is OpenApiSchema openApiSchema && openApiSchema.Type.HasValue)
        {
            openApiSchema.Type = openApiSchema.Type.Value | JsonSchemaType.Null;
        }
    }
}
