using Enumbrella.Sample;

namespace Enumbrella.Tests;

public class IdempotencyTests
{
    [Fact]
    public async Task AddEnumDescriptions_RegisteringTwice_YieldsSameDocument()
    {
        // Given: one host that registers once and one that registers twice
        await using var onceHost = await TestApi.Start<Order>(TestApi.Register);
        await using var twiceHost = await TestApi.Start<Order>(TestApi.RegisterTwice);

        // When: both documents are generated
        var once = await TestApi.Fetch(onceHost);
        var twice = await TestApi.Fetch(twiceHost);

        // Then: the second registration changes nothing
        Assert.Equal(once.ToJsonString(), twice.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_EnumUsedTwice_YieldsOneComponentWithoutDuplicateNames()
    {
        // Given: a response with two properties of the same enum
        await using var host = await TestApi.Start<TwoStatusesResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: both share one component that lists each name once
        var schemas = TestApi.Schemas(document);

        Assert.Equal(
            ["OrderStatus", "TwoStatusesResponse"], schemas.Select(entry => entry.Key).Order());
        TestApi.AssertStatusComponent(document);
    }
}
