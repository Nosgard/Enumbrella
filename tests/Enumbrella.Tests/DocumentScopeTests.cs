using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;

namespace Enumbrella.Tests;

public class DocumentScopeTests
{
    [Fact]
    public async Task AddEnumDescriptions_OneOfTwoDocumentsRegistered_OnlyItCarriesEnumExtensions()
    {
        // Given: a host where v1 has the registration and v2 does not
        await using var app = await StartBothDocuments();

        // When: both documents are fetched
        var registered = await TestApi.Fetch(app, "v1");
        var unregistered = await TestApi.Fetch(app, "v2");

        // Then: v1 lists values and names, and v2 has neither
        TestApi.AssertStatusComponent(registered);
        Assert.Null(unregistered["components"]!["schemas"]!["OrderStatus"]!["enum"]);
        Assert.DoesNotContain("\"x-enum", unregistered.ToJsonString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddEnumDescriptions_NonEnumSchema_IsIdenticalInBothDocuments()
    {
        // Given: a host where v1 has the registration and v2 does not
        await using var app = await StartBothDocuments();

        // When: both documents are fetched
        var registered = await TestApi.Fetch(app, "v1");
        var unregistered = await TestApi.Fetch(app, "v2");

        // Then: the response type, which is no enum, matches character for character
        Assert.Equal(
            unregistered["components"]!["schemas"]!["ScopeResponse"]!.ToJsonString(),
            registered["components"]!["schemas"]!["ScopeResponse"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_ParallelFetches_KeepDocumentsApart()
    {
        // Given: a host where v1 has the registration and v2 does not
        await using var app = await StartBothDocuments();

        // When: 20 parallel fetches alternate between v1 and v2
        var fetches = Enumerable.Range(0, 20)
            .Select(index => TestApi.Fetch(app, $"v{index % 2 + 1}"));
        var documents = await Task.WhenAll(fetches);

        // Then: each v1 has its lists and no v2 has an x-enum extension
        Assert.All(documents.Where((_, index) => index % 2 == 0), TestApi.AssertStatusComponent);
        Assert.All(documents.Where((_, index) => index % 2 == 1), document =>
            Assert.DoesNotContain("\"x-enum", document.ToJsonString(), StringComparison.Ordinal));
    }

    private static Task<WebApplication> StartBothDocuments()
    {
        Action<OpenApiOptions> withoutRegistration = _ => { };

        return TestApi.Start(
            configureJson: null,
            TestApi.MapResponse<ScopeResponse>,
            ("v1", options => options.AddEnumDescriptions()),
            ("v2", withoutRegistration));
    }
}
