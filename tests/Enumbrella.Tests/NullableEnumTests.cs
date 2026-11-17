using System.Text.Json.Nodes;

namespace Enumbrella.Tests;

public class NullableEnumTests
{
    [Fact]
    public async Task AddEnumDescriptions_NullablePropertyAlone_ListsValuesOnComponent()
    {
        // Given: a nullable property as the only use of OrderStatus
        await using var host = await TestApi.Start<NullableStatusResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the component lists the values and both name lists
        AssertOneStatusComponent(document);
    }

    [Fact]
    public async Task AddEnumDescriptions_NullablePropertyWithAndWithoutRegistration_KeepsItsOneOf()
    {
        // Given: one host with the registration and one without
        await using var registeredHost =
            await TestApi.Start<NullableStatusResponse>(TestApi.Register);
        await using var unregisteredHost = await TestApi.Start<NullableStatusResponse>(_ => { });

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: the property keeps its oneOf of null and the reference in both
        var expected = "[{\"type\":\"null\"},{\"$ref\":\"#/components/schemas/OrderStatus\"}]";
        var unregisteredStatus = TestApi.Property(unregistered, "NullableStatusResponse", "status");
        var registeredStatus = TestApi.Property(registered, "NullableStatusResponse", "status");

        Assert.Equal(expected, unregisteredStatus["oneOf"]!.ToJsonString());
        Assert.Equal(expected, registeredStatus["oneOf"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_InlineNullableEnum_ListsNullWithEmptyNames()
    {
        // Given: a nullable enum that stays inline
        await using var host =
            await TestApi.StartInlineEnums<NullableStatusResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the values end with null, which gets an empty name in both lists and an empty
        // description
        TestApi.AssertEnum(
            TestApi.Property(document, "NullableStatusResponse", "status"),
            "[0,1,2,null]",
            "[\"Pending\",\"Approved\",\"Rejected\",\"\"]",
            "[\"Waiting for approval\",\"Approved\",\"\",\"\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_InlineNullableType_StaysAsHostWroteIt()
    {
        // Given: inline enums in one host with the registration and one without
        await using var registeredHost =
            await TestApi.StartInlineEnums<NullableStatusResponse>(TestApi.Register);
        await using var unregisteredHost =
            await TestApi.StartInlineEnums<NullableStatusResponse>(_ => { });

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: both keep the type list the host wrote
        var expected = "[\"null\",\"integer\"]";
        var unregisteredStatus = TestApi.Property(unregistered, "NullableStatusResponse", "status");
        var registeredStatus = TestApi.Property(registered, "NullableStatusResponse", "status");

        Assert.Equal(expected, unregisteredStatus["type"]!.ToJsonString());
        Assert.Equal(expected, registeredStatus["type"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_InlineNullableEnumRegisteredTwice_KeepsOneNull()
    {
        // Given: inline enums in one host registering once and one twice
        await using var onceHost =
            await TestApi.StartInlineEnums<NullableStatusResponse>(TestApi.Register);
        await using var twiceHost =
            await TestApi.StartInlineEnums<NullableStatusResponse>(TestApi.RegisterTwice);

        // When: both documents are generated
        var once = await TestApi.Fetch(onceHost);
        var twice = await TestApi.Fetch(twiceHost);

        // Then: the value list holds one null, each list one entry for it, and the documents match
        TestApi.AssertEnum(
            TestApi.Property(twice, "NullableStatusResponse", "status"),
            "[0,1,2,null]",
            "[\"Pending\",\"Approved\",\"Rejected\",\"\"]",
            "[\"Waiting for approval\",\"Approved\",\"\",\"\"]");
        Assert.Equal(once.ToJsonString(), twice.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_NullablePropertyComesFirst_YieldsOneComponent()
    {
        // Given: a nullable property declared before a plain one of the same enum
        await using var host = await TestApi.Start<NullableFirstResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: both share one component whose values hold no null
        AssertOneStatusComponent(document);
    }

    [Fact]
    public async Task AddEnumDescriptions_NullablePropertyComesLast_YieldsOneComponent()
    {
        // Given: a plain property declared before a nullable one of the same enum
        await using var host = await TestApi.Start<PlainFirstResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: both share one component whose values hold no null
        AssertOneStatusComponent(document);
    }

    [Fact]
    public async Task AddEnumDescriptions_NullableEndpointComesFirst_YieldsOneComponent()
    {
        // Given: a nullable endpoint mapped before a plain one
        await using var host = await TestApi.StartEndpoints(
            TestApi.MapNullableStatus, TestApi.MapPlainStatus);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: both share one component whose values hold no null
        AssertOneStatusComponent(document);
    }

    [Fact]
    public async Task AddEnumDescriptions_NullableEndpointComesLast_YieldsOneComponent()
    {
        // Given: a plain endpoint mapped before a nullable one
        await using var host = await TestApi.StartEndpoints(
            TestApi.MapPlainStatus, TestApi.MapNullableStatus);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: both share one component whose values hold no null
        AssertOneStatusComponent(document);
    }

    private static void AssertOneStatusComponent(JsonNode document)
    {
        var schemas = TestApi.Schemas(document);

        Assert.Single(
            schemas, entry => entry.Key.StartsWith("OrderStatus", StringComparison.Ordinal));
        TestApi.AssertStatusComponent(document);
    }
}
