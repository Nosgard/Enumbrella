using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;

namespace Enumbrella.Tests;

public class PropertyConverterTests
{
    private static readonly Action<OpenApiOptions> Register =
        options => options.AddEnumDescriptions();

    [Fact]
    public async Task string_converter_on_a_property_names_each_string_value()
    {
        // Given: a JsonStringEnumConverter on an inline OrderStatus property

        // When: the document is generated
        var document = await TestApi.InlineEnumDocument<StringPropertyResponse>(Register);

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
        var document = await TestApi.InlineEnumDocument<StringPropertyResponse>(Register);

        // Then: each string keeps its member name and null gets an empty name
        var schema = Property(document, "optional");
        var values = "[\"Pending\",\"Approved\",\"Rejected\",null]";
        var names = "[\"Pending\",\"Approved\",\"Rejected\",\"\"]";

        Assert.Equal(values, schema["enum"]!.ToJsonString());
        Assert.Equal(names, schema["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, schema["x-enumNames"]!.ToJsonString());
    }

    [Fact]
    public async Task string_property_first_adds_only_names_to_the_shared_component()
    {
        // Given: a property with the string converter declared before a plain one

        // When: the document is generated with and without the registration
        var registered = await TestApi.Document<StringFirstResponse>(Register);
        var unregistered = await TestApi.Document<StringFirstResponse>(_ => { });

        // Then: the names follow the strings, and nothing else differs
        var status = registered["components"]!["schemas"]!["OrderStatus"]!;
        var names = "[\"Pending\",\"Approved\",\"Rejected\"]";

        Assert.Equal(names, status["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, status["x-enumNames"]!.ToJsonString());
        Assert.Equal(unregistered.ToJsonString(), WithoutAddedKeywords(registered).ToJsonString());
    }

    [Fact]
    public async Task plain_property_first_adds_only_values_and_names_to_the_shared_component()
    {
        // Given: a plain property declared before one with the string converter

        // When: the document is generated with and without the registration
        var registered = await TestApi.Document<NumberFirstResponse>(Register);
        var unregistered = await TestApi.Document<NumberFirstResponse>(_ => { });

        // Then: the integer values get their names, and nothing else differs
        TestApi.AssertStatusComponent(registered);
        Assert.Equal(unregistered.ToJsonString(), WithoutAddedKeywords(registered).ToJsonString());
    }

    private static JsonNode WithoutAddedKeywords(JsonNode document)
    {
        var copy = document.DeepClone();

        foreach (var schema in Objects(copy).ToList())
        {
            var isInteger = schema["type"]?.ToJsonString() == "\"integer\"";
            var added = schema
                .Select(keyword => keyword.Key)
                .Where(key => key.StartsWith("x-enum", StringComparison.Ordinal)
                    || (isInteger && key == "enum"))
                .ToList();

            added.ForEach(key => schema.Remove(key));
        }

        return copy;
    }

    private static IEnumerable<JsonObject> Objects(JsonNode? node)
    {
        return node switch
        {
            JsonObject obj => [obj, .. obj.SelectMany(keyword => Objects(keyword.Value))],
            JsonArray array => array.SelectMany(Objects),
            _ => [],
        };
    }

    private static JsonNode Property(JsonNode document, string name)
    {
        var response = document["components"]!["schemas"]!["StringPropertyResponse"]!;

        return response["properties"]![name]!;
    }
}
