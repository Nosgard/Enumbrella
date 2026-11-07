using System.Text.Json.Nodes;
using Enumbrella.Sample;
using Microsoft.AspNetCore.Builder;

namespace Enumbrella.Tests;

public class NullableEnumTests
{
    [Fact]
    public async Task nullable_property_alone_lists_values_on_the_component()
    {
        var document = await TestApi.Document<NullableStatusResponse>(
            options => options.AddEnumDescriptions());

        AssertOneStatusComponent(document);
    }

    [Fact]
    public async Task nullable_property_keeps_its_oneOf_with_and_without_registration()
    {
        var registered = await TestApi.Document<NullableStatusResponse>(
            options => options.AddEnumDescriptions());
        var unregistered = await TestApi.Document<NullableStatusResponse>(_ => { });

        var expected = "[{\"type\":\"null\"},{\"$ref\":\"#/components/schemas/OrderStatus\"}]";

        Assert.Equal(expected, StatusProperty(unregistered)["oneOf"]!.ToJsonString());
        Assert.Equal(expected, StatusProperty(registered)["oneOf"]!.ToJsonString());
    }

    [Fact]
    public async Task inline_nullable_enum_ends_its_values_with_null()
    {
        var document = await TestApi.InlineEnumDocument<NullableStatusResponse>(
            options => options.AddEnumDescriptions());

        Assert.Equal("[0,1,2,null]", StatusProperty(document)["enum"]!.ToJsonString());
    }

    [Fact]
    public async Task inline_nullable_enum_has_an_empty_name_for_null()
    {
        var document = await TestApi.InlineEnumDocument<NullableStatusResponse>(
            options => options.AddEnumDescriptions());

        var schema = StatusProperty(document);
        var names = "[\"Pending\",\"Approved\",\"Rejected\",\"\"]";

        Assert.Equal(names, schema["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, schema["x-enumNames"]!.ToJsonString());
    }

    [Fact]
    public async Task inline_nullable_type_stays_as_the_host_wrote_it()
    {
        var registered = await TestApi.InlineEnumDocument<NullableStatusResponse>(
            options => options.AddEnumDescriptions());
        var unregistered = await TestApi.InlineEnumDocument<NullableStatusResponse>(_ => { });

        var expected = "[\"null\",\"integer\"]";

        Assert.Equal(expected, StatusProperty(unregistered)["type"]!.ToJsonString());
        Assert.Equal(expected, StatusProperty(registered)["type"]!.ToJsonString());
    }

    [Fact]
    public async Task registering_twice_keeps_one_null_in_an_inline_enum()
    {
        var once = await TestApi.InlineEnumDocument<NullableStatusResponse>(
            options => options.AddEnumDescriptions());
        var twice = await TestApi.InlineEnumDocument<NullableStatusResponse>(
            options => options.AddEnumDescriptions().AddEnumDescriptions());

        Assert.Equal("[0,1,2,null]", StatusProperty(twice)["enum"]!.ToJsonString());
        Assert.Equal(once.ToJsonString(), twice.ToJsonString());
    }

    [Fact]
    public async Task one_component_when_the_nullable_property_comes_first()
    {
        var document = await TestApi.Document<NullableFirstResponse>(
            options => options.AddEnumDescriptions());

        AssertOneStatusComponent(document);
    }

    [Fact]
    public async Task one_component_when_the_nullable_property_comes_last()
    {
        var document = await TestApi.Document<PlainFirstResponse>(
            options => options.AddEnumDescriptions());

        AssertOneStatusComponent(document);
    }

    [Fact]
    public async Task one_component_when_the_nullable_endpoint_comes_first()
    {
        AssertOneStatusComponent(await EndpointDocument(MapNullableStatus, MapPlainStatus));
    }

    [Fact]
    public async Task one_component_when_the_nullable_endpoint_comes_last()
    {
        AssertOneStatusComponent(await EndpointDocument(MapPlainStatus, MapNullableStatus));
    }

    internal static async Task<JsonNode> EndpointDocument(params Action<WebApplication>[] endpoints)
    {
        await using var app = await TestApi.Start(
            host => Array.ForEach(endpoints, map => map(host)),
            ("v1", options => options.AddEnumDescriptions()));

        return await TestApi.Fetch(app, "v1");
    }

    internal static void MapNullableStatus(WebApplication app)
    {
        app.MapGet("/nullable", () => default(OrderStatus?));
    }

    internal static void MapPlainStatus(WebApplication app)
    {
        app.MapGet("/plain", () => default(OrderStatus));
    }

    private static JsonNode StatusProperty(JsonNode document)
    {
        var response = document["components"]!["schemas"]!["NullableStatusResponse"]!;

        return response["properties"]!["status"]!;
    }

    private static void AssertOneStatusComponent(JsonNode document)
    {
        var schemas = document["components"]!["schemas"]!.AsObject();

        Assert.Single(
            schemas, entry => entry.Key.StartsWith("OrderStatus", StringComparison.Ordinal));
        TestApi.AssertStatusComponent(document);
    }
}
