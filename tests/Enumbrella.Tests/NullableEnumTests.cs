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

    private static async Task<JsonNode> EndpointDocument(params Action<WebApplication>[] endpoints)
    {
        await using var app = await TestApi.Start(
            host => Array.ForEach(endpoints, map => map(host)),
            ("v1", options => options.AddEnumDescriptions()));

        return await TestApi.Fetch(app, "v1");
    }

    private static void MapNullableStatus(WebApplication app)
    {
        app.MapGet("/nullable", () => default(OrderStatus?));
    }

    private static void MapPlainStatus(WebApplication app)
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
        var names = "[\"Pending\",\"Approved\",\"Rejected\"]";

        Assert.Single(
            schemas, entry => entry.Key.StartsWith("OrderStatus", StringComparison.Ordinal));
        Assert.Equal("[0,1,2]", schemas["OrderStatus"]!["enum"]!.ToJsonString());
        Assert.Equal(names, schemas["OrderStatus"]!["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, schemas["OrderStatus"]!["x-enumNames"]!.ToJsonString());
    }
}
