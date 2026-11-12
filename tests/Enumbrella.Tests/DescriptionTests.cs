using System.Text.Json.Nodes;
using Enumbrella.Tests.Descriptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;

namespace Enumbrella.Tests;

public class DescriptionTests
{
    private static readonly Action<OpenApiOptions> Register =
        options => options.AddEnumDescriptions();

    [Fact]
    public async Task AddEnumDescriptions_SomeMembersDescribed_WritesDescriptionsWithEmptyGap()
    {
        // Given: OrderStatus with descriptions on Pending and Approved, none on Rejected
        await using var host = await TestApi.Start<StatusDescriptionsResponse>(Register);

        // When: the document is generated
        var schema = await Schema(host, "OrderStatus");

        // Then: the description list follows enum and leaves Rejected empty
        Assert.Equal(
            "[\"Waiting for approval\",\"Approved\",\"\"]",
            schema["x-enum-descriptions"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_DescriptionWithSpecialCharacters_KeepsTextAfterParsing()
    {
        // Given: descriptions with non-ASCII signs, markup, quotes, a backslash and a line break
        await using var host = await TestApi.Start<SpecialTextResponse>(Register);

        // When: the document is generated and parsed
        var schema = await Schema(host, "SpecialText");

        // Then: each parsed description matches its source text character for character
        var descriptions = schema["x-enum-descriptions"]!.AsArray();

        Assert.Equal("Size ≥ 10 ✓ café", descriptions[0]!.GetValue<string>());
        Assert.Equal(
            "**bold** <script>alert(1)</script> \"quoted\" back\\slash\nnext line",
            descriptions[1]!.GetValue<string>());
    }

    [Fact]
    public async Task AddEnumDescriptions_NoMemberDescribed_WritesNamesWithoutDescriptions()
    {
        // Given: an enum without any description attribute
        await using var host = await TestApi.Start<PriorityResponse>(Register);

        // When: the document is generated
        var schema = await Schema(host, "Priority");

        // Then: the schema has its name list and no description list
        Assert.NotNull(schema["x-enum-varnames"]);
        Assert.Null(schema["x-enum-descriptions"]);
    }

    [Fact]
    public async Task AddEnumDescriptions_LaterDuplicateDescribed_KeepsEmptyFirstDescription()
    {
        // Given: Active and Enabled share the value 1, and only Enabled has a description
        await using var host = await TestApi.Start<SwitchResponse>(Register);

        // When: the document is generated
        var schema = await Schema(host, "Switch");

        // Then: the value 1 keeps Active's name and its empty description
        Assert.Equal("[1,2]", schema["enum"]!.ToJsonString());
        Assert.Equal("[\"Active\",\"Disabled\"]", schema["x-enum-varnames"]!.ToJsonString());
        Assert.Equal("[\"\",\"\"]", schema["x-enum-descriptions"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_UnsortedDescribedEnum_OrdersDescriptionsLikeEnum()
    {
        // Given: Shipping declares Express = 3 before Standard = 1 and Pickup = 2
        await using var host = await TestApi.Start<ShippingAndChannelResponse>(Register);

        // When: the document is generated
        var schema = await Schema(host, "Shipping");

        // Then: each description stands at the position of its value in enum
        Assert.Equal("[1,2,3]", schema["enum"]!.ToJsonString());
        Assert.Equal(
            "[\"Standard delivery\",\"Pickup\",\"Express delivery\"]",
            schema["x-enum-descriptions"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_DescriptionAttributeWithNull_WritesEmptyDescription()
    {
        // Given: Mail carries [Description(null)] and Phone a text
        await using var host = await TestApi.Start<ShippingAndChannelResponse>(Register);

        // When: the document is generated
        var schema = await Schema(host, "Channel");

        // Then: the null description becomes an empty string
        Assert.Equal("[\"\",\"By phone\"]", schema["x-enum-descriptions"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_SeveralDescriptionAttributes_PicksExactThenSingleDerived()
    {
        // Given: members with the exact attribute next to a derived one in both orders, one
        // derived attribute alone, two derived ones and a derived one whose text is null
        await using var host = await TestApi.Start<LabelledResponse>(Register);

        // When: the document is generated
        var schema = await Schema(host, "Labelled");

        // Then: the exact text wins, a single derived one counts, anything else stays empty
        Assert.Equal(
            "[\"A\",\"A\",\"Translated only\",\"\",\"\"]",
            schema["x-enum-descriptions"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_OnlyDerivedAttribute_WritesDescriptions()
    {
        // Given: Localized has a derived description attribute on Card and no exact one anywhere
        await using var host = await TestApi.Start<LabelledResponse>(Register);

        // When: the document is generated
        var schema = await Schema(host, "Localized");

        // Then: the description list exists and leaves Cash empty
        Assert.Equal("[\"Translated card\",\"\"]", schema["x-enum-descriptions"]!.ToJsonString());
    }

    private static async Task<JsonNode> Schema(WebApplication host, string name)
    {
        var document = await TestApi.Fetch(host);

        return document["components"]!["schemas"]![name]!;
    }
}
