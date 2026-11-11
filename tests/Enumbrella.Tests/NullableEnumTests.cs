using System.Text.Json.Nodes;
using Enumbrella.Sample;

namespace Enumbrella.Tests;

public class NullableEnumTests
{
    [Fact]
    public async Task AddEnumDescriptions_NullablePropertyAlone_ListsValuesOnComponent()
    {
        // Given: a nullable property as the only use of OrderStatus

        // When: the document is generated
        var document = await TestApi.Document<NullableStatusResponse>(
            options => options.AddEnumDescriptions());

        // Then: the component lists the values and both name lists
        AssertOneStatusComponent(document);
    }

    [Fact]
    public async Task AddEnumDescriptions_NullablePropertyWithAndWithoutRegistration_KeepsItsOneOf()
    {
        // Given: one host with the registration and one without

        // When: both documents are generated
        var registered = await TestApi.Document<NullableStatusResponse>(
            options => options.AddEnumDescriptions());
        var unregistered = await TestApi.Document<NullableStatusResponse>(_ => { });

        // Then: the property keeps its oneOf of null and the reference in both
        var expected = "[{\"type\":\"null\"},{\"$ref\":\"#/components/schemas/OrderStatus\"}]";

        Assert.Equal(expected, StatusProperty(unregistered)["oneOf"]!.ToJsonString());
        Assert.Equal(expected, StatusProperty(registered)["oneOf"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_InlineNullableEnum_EndsItsValuesWithNull()
    {
        // Given: a nullable enum that stays inline

        // When: the document is generated
        var document = await TestApi.InlineEnumDocument<NullableStatusResponse>(
            options => options.AddEnumDescriptions());

        // Then: its value list ends with null
        Assert.Equal("[0,1,2,null]", StatusProperty(document)["enum"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_InlineNullableEnum_HasEmptyNameForNull()
    {
        // Given: a nullable enum that stays inline

        // When: the document is generated
        var document = await TestApi.InlineEnumDocument<NullableStatusResponse>(
            options => options.AddEnumDescriptions());

        // Then: the null entry has an empty name in both name lists
        var schema = StatusProperty(document);
        var names = "[\"Pending\",\"Approved\",\"Rejected\",\"\"]";

        Assert.Equal(names, schema["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, schema["x-enumNames"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_InlineNullableType_StaysAsHostWroteIt()
    {
        // Given: inline enums in one host with the registration and one without

        // When: both documents are generated
        var registered = await TestApi.InlineEnumDocument<NullableStatusResponse>(
            options => options.AddEnumDescriptions());
        var unregistered = await TestApi.InlineEnumDocument<NullableStatusResponse>(_ => { });

        // Then: both keep the type list the host wrote
        var expected = "[\"null\",\"integer\"]";

        Assert.Equal(expected, StatusProperty(unregistered)["type"]!.ToJsonString());
        Assert.Equal(expected, StatusProperty(registered)["type"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_InlineNullableEnumRegisteredTwice_KeepsOneNull()
    {
        // Given: inline enums in one host registering once and one twice

        // When: both documents are generated
        var once = await TestApi.InlineEnumDocument<NullableStatusResponse>(
            options => options.AddEnumDescriptions());
        var twice = await TestApi.InlineEnumDocument<NullableStatusResponse>(
            options => options.AddEnumDescriptions().AddEnumDescriptions());

        // Then: the value list holds one null and the documents match
        Assert.Equal("[0,1,2,null]", StatusProperty(twice)["enum"]!.ToJsonString());
        Assert.Equal(once.ToJsonString(), twice.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_NullablePropertyComesFirst_YieldsOneComponent()
    {
        // Given: a nullable property declared before a plain one of the same enum

        // When: the document is generated
        var document = await TestApi.Document<NullableFirstResponse>(
            options => options.AddEnumDescriptions());

        // Then: both share one component whose values hold no null
        AssertOneStatusComponent(document);
    }

    [Fact]
    public async Task AddEnumDescriptions_NullablePropertyComesLast_YieldsOneComponent()
    {
        // Given: a plain property declared before a nullable one of the same enum

        // When: the document is generated
        var document = await TestApi.Document<PlainFirstResponse>(
            options => options.AddEnumDescriptions());

        // Then: both share one component whose values hold no null
        AssertOneStatusComponent(document);
    }

    [Fact]
    public async Task AddEnumDescriptions_NullableEndpointComesFirst_YieldsOneComponent()
    {
        // Given: a nullable endpoint mapped before a plain one

        // When: the document is generated
        var document = await TestApi.EndpointDocument(
            TestApi.MapNullableStatus, TestApi.MapPlainStatus);

        // Then: both share one component whose values hold no null
        AssertOneStatusComponent(document);
    }

    [Fact]
    public async Task AddEnumDescriptions_NullableEndpointComesLast_YieldsOneComponent()
    {
        // Given: a plain endpoint mapped before a nullable one

        // When: the document is generated
        var document = await TestApi.EndpointDocument(
            TestApi.MapPlainStatus, TestApi.MapNullableStatus);

        // Then: both share one component whose values hold no null
        AssertOneStatusComponent(document);
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
