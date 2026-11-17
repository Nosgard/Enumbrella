using System.Text.Json.Nodes;
using Enumbrella.Sample;

namespace Enumbrella.Tests;

public class DescriptionTests
{
    [Fact]
    public async Task AddEnumDescriptions_SomeMembersDescribed_WritesDescriptionsWithEmptyGap()
    {
        // Given: OrderStatus with descriptions on Pending and Approved, none on Rejected
        await using var host = await TestApi.Start<Order>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the description list follows enum and leaves Rejected empty
        TestApi.AssertStatusComponent(document);
    }

    [Fact]
    public async Task AddEnumDescriptions_DescriptionWithSpecialCharacters_KeepsTextAfterParsing()
    {
        // Given: descriptions with non-ASCII signs, markup, quotes, a backslash and a line break
        await using var host = await TestApi.Start<SpecialTextResponse>(TestApi.Register);

        // When: the document is generated and parsed
        var document = await TestApi.Fetch(host);

        // Then: each parsed description matches its source text character for character
        var descriptions = new JsonArray(
            "Size ≥ 10 ✓ café",
            "**bold** <script>alert(1)</script> \"quoted\" back\\slash\nnext line");

        TestApi.AssertEnum(
            TestApi.Schema(document, "SpecialText"),
            "[1,2]",
            "[\"Size\",\"Markup\"]",
            descriptions.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_LaterDuplicateDescribed_KeepsEmptyFirstDescription()
    {
        // Given: Active and Enabled share the value 1, and only Enabled has a description
        await using var host = await TestApi.Start<SwitchResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the value 1 keeps Active's name and its empty description
        TestApi.AssertEnum(
            TestApi.Schema(document, "Switch"),
            "[1,2]",
            "[\"Active\",\"Disabled\"]",
            "[\"\",\"\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_UnsortedDescribedEnum_OrdersDescriptionsLikeEnum()
    {
        // Given: Shipping declares Express = 3 before Standard = 1 and Pickup = 2
        await using var host = await TestApi.Start<ShippingResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: each description stands at the position of its value in enum
        TestApi.AssertEnum(
            TestApi.Schema(document, "Shipping"),
            "[1,2,3]",
            "[\"Standard\",\"Pickup\",\"Express\"]",
            "[\"Standard delivery\",\"Pickup\",\"Express delivery\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_DescriptionAttributeWithNull_WritesEmptyDescription()
    {
        // Given: Mail carries [Description(null)] and Phone a text
        await using var host = await TestApi.Start<ChannelResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the null description becomes an empty string
        TestApi.AssertEnum(
            TestApi.Schema(document, "Channel"),
            "[1,2]",
            "[\"Mail\",\"Phone\"]",
            "[\"\",\"By phone\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_SeveralDescriptionAttributes_PicksExactThenSingleDerived()
    {
        // Given: members with the exact attribute next to a derived one in both orders, one
        // derived attribute alone, two derived ones and a derived one whose text is null
        await using var host = await TestApi.Start<LabelledResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the exact text wins, a single derived one counts, anything else stays empty
        TestApi.AssertEnum(
            TestApi.Schema(document, "Labelled"),
            "[1,2,3,4,5]",
            "[\"ExactFirst\",\"ExactLast\",\"DerivedOnly\",\"TwoDerived\",\"NullText\"]",
            "[\"A\",\"A\",\"Translated only\",\"\",\"\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_OnlyDerivedAttribute_WritesDescriptions()
    {
        // Given: Localized has a derived description attribute on Card and no exact one anywhere
        await using var host = await TestApi.Start<LocalizedResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the description list exists and leaves Cash empty
        TestApi.AssertEnum(
            TestApi.Schema(document, "Localized"),
            "[1,2]",
            "[\"Card\",\"Cash\"]",
            "[\"Translated card\",\"\"]");
    }
}
