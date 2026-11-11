using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Member = (string Name, string Description);

namespace Enumbrella;

// Writes the value list, the member names and, when any member carries [Description], the
// descriptions of an enum into its schema. A member without one gets an empty description.
// A missing value list is filled in ascending numeric order, and a value declared twice keeps
// its first member's name and description. The list filled in here ends with null when the
// schema type allows null, with an empty name and description.
// The host writes neither values nor a type for an enum with a custom converter. The transformer
// leaves such a schema untouched and never calls that converter. Holds no state between calls.
internal sealed class EnumSchemaTransformer : IOpenApiSchemaTransformer
{
    private const string VarNamesExtension = "x-enum-varnames";
    private const string EnumNamesExtension = "x-enumNames";
    private const string DescriptionsExtension = "x-enum-descriptions";

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        var enumType = Nullable.GetUnderlyingType(type) ?? type;

        if (!enumType.IsEnum || schema is { Enum: null, Type: null })
        {
            return Task.CompletedTask;
        }

        DescribeValues(schema, ResolveTypeInfo(context), enumType);

        return Task.CompletedTask;
    }

    // A converter on the property decides how its values read and write. It goes into a copy of
    // the options, so the application's options stay as they are.
    private static JsonTypeInfo ResolveTypeInfo(OpenApiSchemaTransformerContext context)
    {
        var converter = context.JsonPropertyInfo?.CustomConverter;

        if (converter is null)
        {
            return context.JsonTypeInfo;
        }

        var options = new JsonSerializerOptions(context.JsonTypeInfo.Options);
        options.Converters.Insert(0, converter);

        return options.GetTypeInfo(context.JsonTypeInfo.Type);
    }

    private static void DescribeValues(OpenApiSchema schema, JsonTypeInfo typeInfo, Type enumType)
    {
        var (byValue, hasDescriptions) = MapMembersByValue(enumType);
        var appendNull = schema.Type?.HasFlag(JsonSchemaType.Null) == true;
        var values = schema.Enum ?? SerializeSortedValues(byValue.Keys, typeInfo, appendNull);

        if (values is null)
        {
            return;
        }

        schema.Enum = values;
        var members = values.Select(value => MemberOf(value, typeInfo, byValue)).ToList();
        WriteLists(schema, members, hasDescriptions);
    }

    // The description list depends on the attribute on any member, also on a later one that
    // shares its value with an earlier member and so never shows its own description.
    private static (Dictionary<object, Member> Members, bool HasDescriptions) MapMembersByValue(
        Type type)
    {
        var members = new Dictionary<object, Member>();
        var hasDescriptions = false;

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var description = DescriptionOf(field);
            var value = Enum.ToObject(type, field.GetRawConstantValue()!);

            hasDescriptions |= description is not null;
            members.TryAdd(value, (field.Name, description ?? ""));
        }

        return (members, hasDescriptions);
    }

    // The exact DescriptionAttribute wins, else a single derived one; several derived ones give
    // an empty text. Null means the field carries no description attribute at all.
    private static string? DescriptionOf(FieldInfo field)
    {
        var attributes = field.GetCustomAttributes<DescriptionAttribute>().ToArray();

        if (attributes.Length == 0)
        {
            return null;
        }

        var exact = attributes.FirstOrDefault(
            attribute => attribute.GetType() == typeof(DescriptionAttribute));
        var chosen = exact ?? (attributes.Length == 1 ? attributes[0] : null);

        return chosen?.Description ?? "";
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

    private static Member MemberOf(
        JsonNode? entry, JsonTypeInfo typeInfo, Dictionary<object, Member> membersByValue)
    {
        if (entry is null)
        {
            return ("", "");
        }

        try
        {
            var value = JsonSerializer.Deserialize(entry, typeInfo);

            return value is null ? ("", "") : membersByValue.GetValueOrDefault(value, ("", ""));
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            return ("", "");
        }
    }

    private static void WriteLists(
        OpenApiSchema schema, List<Member> members, bool hasDescriptions)
    {
        var names = members.Select(member => member.Name).ToList();

        schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        schema.Extensions[VarNamesExtension] = new JsonNodeExtension(ToJsonArray(names));
        schema.Extensions[EnumNamesExtension] = new JsonNodeExtension(ToJsonArray(names));

        if (hasDescriptions)
        {
            var descriptions = ToJsonArray(members.Select(member => member.Description));
            schema.Extensions[DescriptionsExtension] = new JsonNodeExtension(descriptions);
        }
    }

    private static JsonArray ToJsonArray(IEnumerable<string> texts)
    {
        return new JsonArray(texts.Select(text => (JsonNode?)JsonValue.Create(text)).ToArray());
    }
}
