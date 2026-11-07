using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Enumbrella;

internal sealed class EnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
