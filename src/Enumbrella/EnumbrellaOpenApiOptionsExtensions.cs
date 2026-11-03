using Microsoft.AspNetCore.OpenApi;

namespace Enumbrella;

/// <summary>Registers the enum schema transformer on the OpenAPI options of one document.</summary>
public static class EnumbrellaOpenApiOptionsExtensions
{
    /// <summary>
    /// Adds the value list and the member names (<c>x-enum-varnames</c>, <c>x-enumNames</c>)
    /// to every enum schema of the document these options belong to.
    /// </summary>
    /// <param name="options">The options passed by <c>AddOpenApi</c>.</param>
    /// <returns>The same <paramref name="options"/> for further calls.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static OpenApiOptions AddEnumDescriptions(this OpenApiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.AddSchemaTransformer(new EnumSchemaTransformer());

        return options;
    }
}
