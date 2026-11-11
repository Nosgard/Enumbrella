using Enumbrella.Sample;

namespace Enumbrella.Tests;

public class IdempotencyTests
{
    [Fact]
    public async Task AddEnumDescriptions_RegisteringTwice_YieldsSameDocument()
    {
        // Given: one host that registers once and one that registers twice

        // When: both documents are generated
        var once = await TestApi.Document<Order>(options => options.AddEnumDescriptions());
        var twice = await TestApi.Document<Order>(
            options => options.AddEnumDescriptions().AddEnumDescriptions());

        // Then: the second registration changes nothing
        Assert.Equal(once.ToJsonString(), twice.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_EnumUsedTwice_YieldsOneComponentWithoutDuplicateNames()
    {
        // Given: a response with two properties of the same enum

        // When: the document is generated
        var document = await TestApi.Document<TwoStatusesResponse>(
            options => options.AddEnumDescriptions());

        var schemas = document["components"]!["schemas"]!.AsObject();

        // Then: both share one component that lists each name once
        Assert.Equal(
            ["OrderStatus", "TwoStatusesResponse"], schemas.Select(entry => entry.Key).Order());
        TestApi.AssertStatusComponent(document);
    }
}
