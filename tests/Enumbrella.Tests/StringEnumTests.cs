using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Enumbrella.Sample;

namespace Enumbrella.Tests;

public class StringEnumTests
{
    [Fact]
    public async Task AddEnumDescriptions_CamelCaseStrings_NamesAndDescriptionsFollowEachString()
    {
        // Given: OrderStatus as camelCase strings, with descriptions on Pending and Approved
        await using var host = await TestApi.Start<Order>(TestApi.Register, CamelCaseStrings);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: each string gets its C# name and its description at the same position
        TestApi.AssertEnum(
            TestApi.Schema(document, "OrderStatus"),
            "[\"pending\",\"approved\",\"rejected\"]",
            "[\"Pending\",\"Approved\",\"Rejected\"]",
            "[\"Waiting for approval\",\"Approved\",\"\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_RenamedStringMember_KeepsCSharpNameAndDescription()
    {
        // Given: ReviewStatus as camelCase strings, with Rejected described and renamed "declined"
        await using var host =
            await TestApi.Start<ReviewStatusResponse>(TestApi.Register, CamelCaseStrings);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: "declined" gets the C# name Rejected and its description
        TestApi.AssertEnum(
            TestApi.Schema(document, "ReviewStatus"),
            "[\"pending\",\"approved\",\"declined\"]",
            "[\"Pending\",\"Approved\",\"Rejected\"]",
            "[\"\",\"\",\"Declined by review\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_ForeignUndeclaredString_GetsEmptyNameAndDescription()
    {
        // Given: camelCase strings with a schema transformer that writes pending, the undeclared
        // "7" and approved before the registration
        await using var host = await TestApi.Start<Order>(
            TestApi.RegisterAfterEntries<OrderStatus>("[\"pending\",\"7\",\"approved\"]"),
            CamelCaseStrings);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: "7" gets an empty name and description, and the other strings keep their own
        TestApi.AssertEnum(
            TestApi.Schema(document, "OrderStatus"),
            "[\"pending\",\"7\",\"approved\"]",
            "[\"Pending\",\"\",\"Approved\"]",
            "[\"Waiting for approval\",\"\",\"Approved\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_NullableCamelCaseStrings_ListsEmptyEntriesAtNull()
    {
        // Given: OrderStatus as camelCase strings, used only as an OrderStatus? property
        await using var host =
            await TestApi.Start<NullableStatusResponse>(TestApi.Register, CamelCaseStrings);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: null in enum gets an empty name in both lists and an empty description
        TestApi.AssertEnum(
            TestApi.Schema(document, "OrderStatus"),
            "[\"pending\",\"approved\",\"rejected\",null]",
            "[\"Pending\",\"Approved\",\"Rejected\",\"\"]",
            "[\"Waiting for approval\",\"Approved\",\"\",\"\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_StringsPlainFirst_ListsWithoutNullLikeHost()
    {
        // Given: camelCase strings with OrderStatus before OrderStatus?, with and without the
        // registration
        await using var registeredHost =
            await TestApi.Start<PlainFirstResponse>(TestApi.Register, CamelCaseStrings);
        await using var unregisteredHost =
            await TestApi.Start<PlainFirstResponse>(_ => { }, CamelCaseStrings);

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: one OrderStatus component matches the host without the registration, and it lists
        // names and descriptions without null
        AssertStatusLikeUnregistered(registered, unregistered, "PlainFirstResponse");
        TestApi.AssertEnum(
            TestApi.Schema(registered, "OrderStatus"),
            "[\"pending\",\"approved\",\"rejected\"]",
            "[\"Pending\",\"Approved\",\"Rejected\"]",
            "[\"Waiting for approval\",\"Approved\",\"\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_StringsNullableFirst_ListsEmptyEntriesAtNullLikeHost()
    {
        // Given: camelCase strings with OrderStatus? before OrderStatus, with and without the
        // registration
        await using var registeredHost =
            await TestApi.Start<NullableFirstResponse>(TestApi.Register, CamelCaseStrings);
        await using var unregisteredHost =
            await TestApi.Start<NullableFirstResponse>(_ => { }, CamelCaseStrings);

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: one OrderStatus component matches the host without the registration, and it lists
        // empty entries at null
        AssertStatusLikeUnregistered(registered, unregistered, "NullableFirstResponse");
        TestApi.AssertEnum(
            TestApi.Schema(registered, "OrderStatus"),
            "[\"pending\",\"approved\",\"rejected\",null]",
            "[\"Pending\",\"Approved\",\"Rejected\",\"\"]",
            "[\"Waiting for approval\",\"Approved\",\"\",\"\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_InlineCamelCaseStrings_ListLikeComponent()
    {
        // Given: camelCase strings that stay inline, as OrderStatus and as OrderStatus?
        await using var host =
            await TestApi.StartInlineEnums<PlainFirstResponse>(TestApi.Register, CamelCaseStrings);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: both properties carry the names and descriptions of the component, the nullable
        // one with empty entries at null
        TestApi.AssertEnum(
            TestApi.Property(document, "PlainFirstResponse", "first"),
            "[\"pending\",\"approved\",\"rejected\"]",
            "[\"Pending\",\"Approved\",\"Rejected\"]",
            "[\"Waiting for approval\",\"Approved\",\"\"]");
        TestApi.AssertEnum(
            TestApi.Property(document, "PlainFirstResponse", "second"),
            "[\"pending\",\"approved\",\"rejected\",null]",
            "[\"Pending\",\"Approved\",\"Rejected\",\"\"]",
            "[\"Waiting for approval\",\"Approved\",\"\",\"\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_StringsWithoutNamingPolicy_NamesMatchStrings()
    {
        // Given: OrderStatus as strings without a naming policy
        await using var host = await TestApi.Start<Order>(
            TestApi.Register, json => json.Converters.Add(new JsonStringEnumConverter()));

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: both name lists repeat the strings, and the descriptions stand at their position
        var names = "[\"Pending\",\"Approved\",\"Rejected\"]";

        TestApi.AssertEnum(
            TestApi.Schema(document, "OrderStatus"),
            names,
            names,
            "[\"Waiting for approval\",\"Approved\",\"\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_StringsWithoutDescriptions_HaveNoDescriptionList()
    {
        // Given: Priority as strings without a naming policy, with no description on any member
        await using var host = await TestApi.Start<PriorityResponse>(
            TestApi.Register, json => json.Converters.Add(new JsonStringEnumConverter()));

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the strings follow the values with matching names, and no description list exists
        var names = "[\"Low\",\"Medium\",\"High\"]";

        TestApi.AssertEnum(TestApi.Schema(document, "Priority"), names, names, null);
    }

    [Fact]
    public async Task AddEnumDescriptions_SnakeCaseStrings_ListCSharpNames()
    {
        // Given: a snake_case converter that writes PaymentMethod.BankTransfer as bank_transfer
        await using var host = await TestApi.Start<Payment>(
            TestApi.Register,
            json => json.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: each string gets its C# name and its description at the same position
        TestApi.AssertEnum(
            TestApi.Schema(document, "PaymentMethod"),
            "[\"card\",\"invoice\",\"bank_transfer\"]",
            "[\"Card\",\"Invoice\",\"BankTransfer\"]",
            "[\"Paid by card\",\"Paid on invoice\",\"Paid by bank transfer\"]");
    }

    private static void CamelCaseStrings(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    private static void AssertStatusLikeUnregistered(
        JsonNode registered, JsonNode unregistered, string response)
    {
        var registeredStatus = TestApi.Schema(registered, "OrderStatus");
        var unregisteredStatus = TestApi.Schema(unregistered, "OrderStatus");

        TestApi.AssertSingleComponent(registered, "OrderStatus");
        Assert.Equal(
            unregisteredStatus["type"]?.ToJsonString(), registeredStatus["type"]?.ToJsonString());
        Assert.Equal(
            unregisteredStatus["enum"]!.ToJsonString(), registeredStatus["enum"]!.ToJsonString());
        Assert.Equal(
            TestApi.Schema(unregistered, response).ToJsonString(),
            TestApi.Schema(registered, response).ToJsonString());
    }
}
