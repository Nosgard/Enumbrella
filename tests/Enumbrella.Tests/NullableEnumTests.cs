using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;

namespace Enumbrella.Tests;

public class NullableEnumTests
{
    [Fact]
    public async Task inline_nullable_enum_lists_values_with_null_last()
    {
        var schema = await InlineStatusSchema(options => options.AddEnumDescriptions());

        var types = schema["type"]!.AsArray().Select(t => t!.GetValue<string>()).Order();

        Assert.Equal(["integer", "null"], types);
        Assert.Equal("[0,1,2,null]", schema["enum"]!.ToJsonString());
    }

    [Fact]
    public async Task inline_nullable_enum_names_end_with_an_empty_entry()
    {
        const string names = "[\"Pending\",\"Approved\",\"Rejected\",\"\"]";
        var schema = await InlineStatusSchema(options => options.AddEnumDescriptions());

        Assert.Equal(names, schema["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, schema["x-enumNames"]!.ToJsonString());
    }

    [Fact]
    public async Task registering_twice_keeps_one_null()
    {
        var schema = await InlineStatusSchema(
            options => options.AddEnumDescriptions().AddEnumDescriptions());

        Assert.Equal("[0,1,2,null]", schema["enum"]!.ToJsonString());
        Assert.Equal(4, schema["x-enum-varnames"]!.AsArray().Count);
    }

    internal static void KeepEnumsInline(OpenApiOptions options)
    {
        options.CreateSchemaReferenceId = type =>
            (Nullable.GetUnderlyingType(type.Type) ?? type.Type).IsEnum
                ? null
                : OpenApiOptions.CreateDefaultSchemaReferenceId(type);
    }

    private static async Task<JsonNode> InlineStatusSchema(Action<OpenApiOptions> register)
    {
        var document = await TestApi.Document<NullableStatusResponse>(options =>
        {
            register(options);
            KeepEnumsInline(options);
        });

        var response = document["components"]!["schemas"]!["NullableStatusResponse"]!;

        return response["properties"]!["status"]!;
    }
}
