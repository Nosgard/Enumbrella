using System.Text.Json.Nodes;
using Enumbrella.Tests.Descriptions;

namespace Enumbrella.Tests;

public class DescriptionTests
{
    [Fact]
    public async Task AddEnumDescriptions_SomeMembersDescribed_WritesDescriptionsWithEmptyGap()
    {
        // Given: OrderStatus with descriptions on Pending and Approved, none on Rejected

        // When: the document is generated
        var schema = await Schema<StatusDescriptionsResponse>("OrderStatus");

        // Then: the description list follows enum and leaves Rejected empty
        Assert.Equal(
            "[\"Waiting for approval\",\"Approved\",\"\"]",
            schema["x-enum-descriptions"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_DescriptionWithSpecialCharacters_KeepsTextAfterParsing()
    {
        // Given: descriptions with non-ASCII signs, markup, quotes, a backslash and a line break

        // When: the document is generated and parsed
        var schema = await Schema<SpecialTextResponse>("SpecialText");

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

        // When: the document is generated
        var schema = await Schema<PriorityResponse>("Priority");

        // Then: the schema has its name list and no description list
        Assert.NotNull(schema["x-enum-varnames"]);
        Assert.Null(schema["x-enum-descriptions"]);
    }

    [Fact]
    public async Task AddEnumDescriptions_LaterDuplicateDescribed_KeepsEmptyFirstDescription()
    {
        // Given: Active and Enabled share the value 1, and only Enabled has a description

        // When: the document is generated
        var schema = await Schema<SwitchResponse>("Switch");

        // Then: the value 1 keeps Active's name and its empty description
        Assert.Equal("[1,2]", schema["enum"]!.ToJsonString());
        Assert.Equal("[\"Active\",\"Disabled\"]", schema["x-enum-varnames"]!.ToJsonString());
        Assert.Equal("[\"\",\"\"]", schema["x-enum-descriptions"]!.ToJsonString());
    }

    private static async Task<JsonNode> Schema<TResponse>(string name)
    {
        var document = await TestApi.Document<TResponse>(options => options.AddEnumDescriptions());

        return document["components"]!["schemas"]![name]!;
    }
}
