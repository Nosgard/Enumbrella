namespace Enumbrella.Tests;

public class PropertyConverterTests
{
    [Fact]
    public async Task AddEnumDescriptions_StringConverterOnProperty_NamesEachStringValue()
    {
        // Given: a JsonStringEnumConverter on an inline OrderStatus property
        await using var host =
            await TestApi.StartInlineEnums<StringPropertyResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the strings in enum and both name lists match the member names, with descriptions
        var names = "[\"Pending\",\"Approved\",\"Rejected\"]";

        TestApi.AssertEnum(
            TestApi.Property(document, "StringPropertyResponse", "plain"),
            names,
            names,
            "[\"Waiting for approval\",\"Approved\",\"\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_StringConverterOnNullableProperty_GivesNullEmptyName()
    {
        // Given: a JsonStringEnumConverter on an inline OrderStatus? property
        await using var host =
            await TestApi.StartInlineEnums<NullableStringPropertyResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: each string keeps its member name and null gets an empty name and description
        TestApi.AssertEnum(
            TestApi.Property(document, "NullableStringPropertyResponse", "optional"),
            "[\"Pending\",\"Approved\",\"Rejected\",null]",
            "[\"Pending\",\"Approved\",\"Rejected\",\"\"]",
            "[\"Waiting for approval\",\"Approved\",\"\",\"\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_StringPropertyFirst_AddsOnlyListsToSharedComponent()
    {
        // Given: a property with the string converter declared before a plain one
        await using var registeredHost = await TestApi.Start<StringFirstResponse>(TestApi.Register);
        await using var unregisteredHost = await TestApi.Start<StringFirstResponse>(_ => { });

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: the names and descriptions follow the strings, and nothing else differs
        var names = "[\"Pending\",\"Approved\",\"Rejected\"]";

        TestApi.AssertEnum(
            TestApi.Schema(registered, "OrderStatus"),
            names,
            names,
            "[\"Waiting for approval\",\"Approved\",\"\"]");
        Assert.Equal(
            unregistered.ToJsonString(), TestApi.WithoutAddedKeywords(registered).ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_PlainPropertyFirst_AddsOnlyEnumAndListsToSharedComponent()
    {
        // Given: a plain property declared before one with the string converter
        await using var registeredHost = await TestApi.Start<NumberFirstResponse>(TestApi.Register);
        await using var unregisteredHost = await TestApi.Start<NumberFirstResponse>(_ => { });

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: the integer values get their names, and nothing else differs
        TestApi.AssertStatusComponent(registered);
        Assert.Equal(
            unregistered.ToJsonString(), TestApi.WithoutAddedKeywords(registered).ToJsonString());
    }
}
