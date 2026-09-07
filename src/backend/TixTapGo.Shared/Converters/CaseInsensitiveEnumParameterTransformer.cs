using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace TixTapGo.Shared.Converters;

/// <summary>
/// Rewrites the OpenAPI schema for <see cref="CaseInsensitiveEnum{T}"/>
/// </summary>
public class CaseInsensitiveEnumParameterTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var paramDescription in context.Description.ParameterDescriptions)
        {
            var elementType = paramDescription.Type.IsArray
                ? paramDescription.Type.GetElementType()
                : paramDescription.Type;
            if (elementType is not { IsGenericType: true } ||
                elementType.GetGenericTypeDefinition() != typeof(CaseInsensitiveEnum<>))
            {
                continue;
            }

            var enumType = elementType.GetGenericArguments()[0];
            var enumSchema = await context.GetOrCreateSchemaAsync(enumType, cancellationToken: cancellationToken);

            if (operation.Parameters?
                    .FirstOrDefault(p => p.Name == paramDescription.Name) is not OpenApiParameter concreteParam)
            {
                continue;
            }

            concreteParam.Schema = paramDescription.Type.IsArray
                ? new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = enumSchema
                }
                : enumSchema;
        }
    }
}
