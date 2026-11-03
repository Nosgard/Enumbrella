using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Enumbrella;

/// <summary>
/// Writes the value list and the member names of an enum into its schema.
/// A missing value list is filled in ascending numeric order; a value declared twice keeps the
/// name of its first member. An enum whose converter cannot write its values is left untouched.
/// The host calls it once per schema; it keeps no state between calls.
/// </summary>
internal sealed class EnumSchemaTransformer : IOpenApiSchemaTransformer
{
    internal const string VarNamesExtension = "x-enum-varnames";
    internal const string EnumNamesExtension = "x-enumNames";

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var typeInfo = context.JsonTypeInfo;
        var type = Nullable.GetUnderlyingType(typeInfo.Type) ?? typeInfo.Type;

        if (!type.IsEnum)
        {
            return Task.CompletedTask;
        }

        var namesByValue = NamesByValue(type);
        var values = schema.Enum ?? Values(namesByValue.Keys, typeInfo);

        if (values is not null)
        {
            var names = values.Select(value => NameOf(value, typeInfo, namesByValue)).ToList();

            schema.Enum = values;
            WriteNames(schema, names);
        }

        return Task.CompletedTask;
    }

    private static Dictionary<object, string> NamesByValue(Type type)
    {
        var names = new Dictionary<object, string>();

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            names.TryAdd(Enum.ToObject(type, field.GetRawConstantValue()!), field.Name);
        }

        return names;
    }

    private static List<JsonNode>? Values(IEnumerable<object> values, JsonTypeInfo typeInfo)
    {
        try
        {
            return values.Order()
                .Select(value => JsonSerializer.SerializeToNode(value, typeInfo)!)
                .ToList();
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static string NameOf(
        JsonNode? entry, JsonTypeInfo typeInfo, Dictionary<object, string> namesByValue)
    {
        if (entry is null)
        {
            return "";
        }

        try
        {
            var value = JsonSerializer.Deserialize(entry, typeInfo);

            return value is null ? "" : namesByValue.GetValueOrDefault(value, "");
        }
        catch (JsonException)
        {
            return "";
        }
        catch (NotSupportedException)
        {
            return "";
        }
    }

    private static void WriteNames(OpenApiSchema schema, List<string> names)
    {
        schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        schema.Extensions[VarNamesExtension] = new JsonNodeExtension(ToJsonArray(names));
        schema.Extensions[EnumNamesExtension] = new JsonNodeExtension(ToJsonArray(names));
    }

    private static JsonArray ToJsonArray(List<string> names)
    {
        return new JsonArray(names.Select(name => (JsonNode?)JsonValue.Create(name)).ToArray());
    }
}
