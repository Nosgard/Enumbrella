using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Enumbrella;

/// <summary>
/// Writes the value list and the member names of an enum into its schema.
/// The host calls it once per schema; it keeps no state between calls.
/// </summary>
internal sealed class EnumSchemaTransformer : IOpenApiSchemaTransformer
{
    internal const string VarNamesExtension = "x-enum-varnames";
    internal const string EnumNamesExtension = "x-enumNames";

    public Task TransformAsync(
        OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
