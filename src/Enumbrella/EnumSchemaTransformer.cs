using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Enumbrella;

// Writes the value list and the member names of an enum into its schema. A missing value list
// is filled in ascending numeric order, and a value declared twice keeps its first member's name.
// The list filled in here ends with null when the schema type allows null, with an empty name.
// The host writes neither values nor a type for an enum with a custom converter. The transformer
// leaves such a schema untouched and never calls that converter. Holds no state between calls.
internal sealed class EnumSchemaTransformer : IOpenApiSchemaTransformer
{
    private const string VarNamesExtension = "x-enum-varnames";
    private const string EnumNamesExtension = "x-enumNames";

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var typeInfo = context.JsonTypeInfo;
        var enumType = Nullable.GetUnderlyingType(typeInfo.Type) ?? typeInfo.Type;

        if (!enumType.IsEnum || schema is { Enum: null, Type: null })
        {
            return Task.CompletedTask;
        }

        DescribeValues(schema, typeInfo, enumType);

        return Task.CompletedTask;
    }

    private static void DescribeValues(OpenApiSchema schema, JsonTypeInfo typeInfo, Type enumType)
    {
        var namesByValue = MapNamesByValue(enumType);
        var appendNull = schema.Type?.HasFlag(JsonSchemaType.Null) == true;
        var values = schema.Enum ?? SerializeSortedValues(namesByValue.Keys, typeInfo, appendNull);

        if (values is null)
        {
            return;
        }

        schema.Enum = values;
        WriteNames(schema, values.Select(value => NameOf(value, typeInfo, namesByValue)).ToList());
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
        IEnumerable<object> values, JsonTypeInfo typeInfo, bool appendNull)
    {
        try
        {
            var nodes = values.Order()
                .Select(value => JsonSerializer.SerializeToNode(value, typeInfo)!)
                .ToList();

            return appendNull ? [.. nodes, null!] : nodes;
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
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
        catch (Exception e) when (e is JsonException or NotSupportedException)
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
