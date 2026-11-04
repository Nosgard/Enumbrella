using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Enumbrella;

// Writes the value list and the member names of an enum into its schema. A missing value list
// is filled in ascending numeric order, with null last when the schema type allows null, and a
// value declared twice keeps its first member's name. An enum whose converter cannot write its
// values stays untouched. Holds no state between calls.
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

        var namesByValue = MapNamesByValue(type);
        var values = schema.Enum ?? SerializeSortedValues(namesByValue.Keys, typeInfo, schema);

        if (values is null)
        {
            return Task.CompletedTask;
        }

        schema.Enum = values;
        WriteNames(schema, values.Select(value => NameOf(value, typeInfo, namesByValue)).ToList());

        return Task.CompletedTask;
    }

    private static Dictionary<object, string> MapNamesByValue(Type type)
    {
        var names = new Dictionary<object, string>();

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            names.TryAdd(Enum.ToObject(type, field.GetRawConstantValue()!), field.Name);
        }

        return names;
    }

    private static List<JsonNode>? SerializeSortedValues(
        IEnumerable<object> values, JsonTypeInfo typeInfo, OpenApiSchema schema)
    {
        try
        {
            var nodes = values.Order()
                .Select(value => JsonSerializer.SerializeToNode(value, typeInfo)!)
                .ToList();

            if (schema.Type?.HasFlag(JsonSchemaType.Null) == true)
            {
                nodes.Add(null!);
            }

            return nodes;
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
