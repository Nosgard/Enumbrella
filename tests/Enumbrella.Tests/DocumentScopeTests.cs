using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;

namespace Enumbrella.Tests;

public class DocumentScopeTests
{
    [Fact]
    public async Task only_the_registered_document_carries_enum_extensions()
    {
        await using var app = await StartBothDocuments();

        var registered = await TestApi.Fetch(app, "v1");
        var unregistered = await TestApi.Fetch(app, "v2");

        TestApi.AssertStatusComponent(registered);
        Assert.Null(unregistered["components"]!["schemas"]!["OrderStatus"]!["enum"]);
        Assert.DoesNotContain("\"x-enum", unregistered.ToJsonString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task a_non_enum_schema_is_identical_in_both_documents()
    {
        await using var app = await StartBothDocuments();

        var registered = await TestApi.Fetch(app, "v1");
        var unregistered = await TestApi.Fetch(app, "v2");

        Assert.Equal(
            unregistered["components"]!["schemas"]!["ScopeResponse"]!.ToJsonString(),
            registered["components"]!["schemas"]!["ScopeResponse"]!.ToJsonString());
    }

    [Fact]
    public async Task parallel_fetches_keep_the_documents_apart()
    {
        await using var app = await StartBothDocuments();

        var fetches = Enumerable.Range(0, 20)
            .Select(index => TestApi.Fetch(app, $"v{index % 2 + 1}"));
        var documents = await Task.WhenAll(fetches);

        Assert.All(documents.Where((_, index) => index % 2 == 0), TestApi.AssertStatusComponent);
        Assert.All(documents.Where((_, index) => index % 2 == 1), document =>
            Assert.DoesNotContain("\"x-enum", document.ToJsonString(), StringComparison.Ordinal));
    }

    private static Task<WebApplication> StartBothDocuments()
    {
        Action<OpenApiOptions> withoutRegistration = _ => { };

        return TestApi.Start(
            TestApi.MapResponse<ScopeResponse>,
            ("v1", options => options.AddEnumDescriptions()),
            ("v2", withoutRegistration));
    }
}
