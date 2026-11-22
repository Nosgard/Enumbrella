using System.Text.Json.Nodes;
using Enumbrella.Sample;

namespace Enumbrella.Tests;

public class LocationTests
{
    [Fact]
    public async Task AddEnumDescriptions_StatusAtEveryLocation_SharesOneListedComponent()
    {
        // Given: OrderStatus as property, path parameter, query parameter and body element
        await using var host = await TestApi.StartLocations<Order, OrderStatus>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: each location refers to the one OrderStatus component, which lists each value once
        var reference = "{\"$ref\":\"#/components/schemas/OrderStatus\"}";

        Assert.Equal(reference, TestApi.Property(document, "Order", "status").ToJsonString());
        Assert.All(
            TestApi.LocationSchemas(document),
            schema => Assert.Equal(reference, schema.ToJsonString()));
        TestApi.AssertOneStatusComponent(document);
    }

    [Fact]
    public async Task AddEnumDescriptions_InlineStatusAtEveryLocation_ListsAtEachLocation()
    {
        // Given: OrderStatus inline as property, path parameter, query parameter and body element
        await using var host =
            await TestApi.StartInlineLocations<Order, OrderStatus>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: each location carries the values, both name lists and the descriptions
        var values = "[0,1,2]";
        var names = "[\"Pending\",\"Approved\",\"Rejected\"]";
        var descriptions = "[\"Waiting for approval\",\"Approved\",\"\"]";

        TestApi.AssertEnum(
            TestApi.Property(document, "Order", "status"), values, names, descriptions);
        Assert.All(
            TestApi.LocationSchemas(document),
            schema => TestApi.AssertEnum(schema, values, names, descriptions));
    }

    [Fact]
    public async Task AddEnumDescriptions_NullableQueryParameter_KeepsNullabilityOfHost()
    {
        // Given: OrderStatus? only as query parameter, so the component comes from it, in one host
        // with the registration and one without
        await using var registeredHost = await TestApi.StartNullableQuery(TestApi.Register);
        await using var unregisteredHost = await TestApi.StartNullableQuery(_ => { });

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: parameter and component match the host apart from the added keywords, and the
        // component lists the values without null
        Assert.Equal(
            TestApi.Parameter(unregistered, "/nullable-query", "status").ToJsonString(),
            TestApi.Parameter(registered, "/nullable-query", "status").ToJsonString());
        Assert.Equal(
            TestApi.Schema(unregistered, "OrderStatus").ToJsonString(),
            TestApi.WithoutAddedKeywords(TestApi.Schema(registered, "OrderStatus")).ToJsonString());
        TestApi.AssertStatusComponent(registered);
    }

    [Fact]
    public async Task AddEnumDescriptions_InlineNullableQueryParameter_KeepsNullabilityOfHost()
    {
        // Given: OrderStatus? inline as query parameter, in hosts with and without the registration
        await using var registeredHost =
            await TestApi.StartInlineLocations<Order, OrderStatus>(TestApi.Register);
        await using var unregisteredHost =
            await TestApi.StartInlineLocations<Order, OrderStatus>(_ => { });

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: the parameter matches without the added keywords, and no list has an entry for null
        var registeredQuery = TestApi.Parameter(registered, "/nullable-query", "status");
        var unregisteredQuery = TestApi.Parameter(unregistered, "/nullable-query", "status");
        var names = "[\"Pending\",\"Approved\",\"Rejected\"]";
        var descriptions = "[\"Waiting for approval\",\"Approved\",\"\"]";

        Assert.Equal(
            unregisteredQuery.ToJsonString(),
            TestApi.WithoutAddedKeywords(registeredQuery).ToJsonString());
        TestApi.AssertEnum(registeredQuery["schema"]!, "[0,1,2]", names, descriptions);
    }

    [Fact]
    public async Task AddEnumDescriptions_NumbersAtLocations_StayAsHostWroteThem()
    {
        // Given: int beside OrderStatus at path, query and body, with and without the registration
        await using var registeredHost =
            await TestApi.StartLocations<Order, OrderStatus>(TestApi.Register);
        await using var unregisteredHost =
            await TestApi.StartLocations<Order, OrderStatus>(_ => { });

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: each int location matches the one without the registration character for character
        AssertNumbersLikeUnregistered(registered, unregistered);
    }

    [Fact]
    public async Task AddEnumDescriptions_InlineNumbersAtLocations_StayAsHostWroteThem()
    {
        // Given: int beside inline OrderStatus at path, query and body, registered and unregistered
        await using var registeredHost =
            await TestApi.StartInlineLocations<Order, OrderStatus>(TestApi.Register);
        await using var unregisteredHost =
            await TestApi.StartInlineLocations<Order, OrderStatus>(_ => { });

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: each int location matches the one without the registration character for character
        AssertNumbersLikeUnregistered(registered, unregistered);
    }

    [Fact]
    public async Task AddEnumDescriptions_ApiWithoutEnums_MatchesUnregisteredDocument()
    {
        // Given: int as path parameter, query parameter and body element and no enum, in one host
        // with the registration and one without
        await using var registeredHost = await TestApi.StartLocations<int>(TestApi.Register);
        await using var unregisteredHost = await TestApi.StartLocations<int>(_ => { });

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: both documents match character for character
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_EnumsAtEveryLocation_DifferOnlyInAddedKeywords()
    {
        // Given: the described OrderStatus at every location, with OrderStatus? and the string
        // enum Shade in the response, in one host with the registration and one without
        await using var registeredHost =
            await TestApi.StartLocations<MixedResponse, OrderStatus>(TestApi.Register);
        await using var unregisteredHost =
            await TestApi.StartLocations<MixedResponse, OrderStatus>(_ => { });

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: without its x-enum extensions and the enum of integer schemas, the document with
        // the registration matches the one without
        Assert.Equal(
            unregistered.ToJsonString(), TestApi.WithoutAddedKeywords(registered).ToJsonString());
    }

    private static void AssertNumbersLikeUnregistered(JsonNode registered, JsonNode unregistered)
    {
        Assert.Equal(
            TestApi.Parameter(unregistered, "/path/{status}/{id}", "id").ToJsonString(),
            TestApi.Parameter(registered, "/path/{status}/{id}", "id").ToJsonString());
        Assert.Equal(
            TestApi.Parameter(unregistered, "/query", "id").ToJsonString(),
            TestApi.Parameter(registered, "/query", "id").ToJsonString());
        Assert.Equal(
            TestApi.BodyItems(unregistered, "/numbers").ToJsonString(),
            TestApi.BodyItems(registered, "/numbers").ToJsonString());
    }
}
