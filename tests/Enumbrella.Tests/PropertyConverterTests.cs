using System.Text.Json.Nodes;

namespace Enumbrella.Tests;

public class PropertyConverterTests
{
    [Fact]
    public async Task string_converter_on_a_property_names_each_string_value()
    {
        // Given: a JsonStringEnumConverter on an inline OrderStatus property

        // When: the document is generated
        var document = await TestApi.InlineEnumDocument<StringPropertyResponse>(
            options => options.AddEnumDescriptions());

        // Then: the strings in enum and both name lists match the member names
        var schema = Property(document, "plain");
        var names = "[\"Pending\",\"Approved\",\"Rejected\"]";

        Assert.Equal(names, schema["enum"]!.ToJsonString());
        Assert.Equal(names, schema["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, schema["x-enumNames"]!.ToJsonString());
    }

    [Fact]
    public async Task string_converter_on_a_nullable_property_gives_null_an_empty_name()
    {
        // Given: a JsonStringEnumConverter on an inline OrderStatus? property

        // When: the document is generated
        var document = await TestApi.InlineEnumDocument<StringPropertyResponse>(
            options => options.AddEnumDescriptions());

        // Then: each string keeps its member name and null gets an empty name
        var schema = Property(document, "optional");
        var values = "[\"Pending\",\"Approved\",\"Rejected\",null]";
        var names = "[\"Pending\",\"Approved\",\"Rejected\",\"\"]";

        Assert.Equal(values, schema["enum"]!.ToJsonString());
        Assert.Equal(names, schema["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, schema["x-enumNames"]!.ToJsonString());
    }

    private static JsonNode Property(JsonNode document, string name)
    {
        var response = document["components"]!["schemas"]!["StringPropertyResponse"]!;

        return response["properties"]![name]!;
    }
}
