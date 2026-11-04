using System.Text.Json.Nodes;
using Enumbrella.Sample;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;

namespace Enumbrella.Tests;

public class DocumentScopeTests
{
    [Fact]
    public async Task only_the_registered_document_carries_enum_extensions()
    {
        await using var app = await StartTwoDocuments();

        var v1 = await TestApi.Fetch(app, "v1");
        var v2 = await TestApi.Fetch(app, "v2");

        Assert.NotNull(Schemas(v1)["OrderStatus"]!["x-enum-varnames"]);
        Assert.Null(Schemas(v2)["OrderStatus"]!["enum"]);
        AssertNoEnumExtensions(v2);
    }

    [Fact]
    public async Task a_non_enum_schema_is_identical_in_both_documents()
    {
        await using var app = await StartTwoDocuments();

        var v1 = await TestApi.Fetch(app, "v1");
        var v2 = await TestApi.Fetch(app, "v2");

        Assert.Equal(Schemas(v1)["Order"]!.ToJsonString(), Schemas(v2)["Order"]!.ToJsonString());
    }

    [Fact]
    public async Task parallel_requests_keep_documents_apart()
    {
        await using var app = await StartTwoDocuments();

        var documents = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(i => TestApi.Fetch(app, i % 2 == 0 ? "v1" : "v2")));

        var v1Documents = documents.Where((_, i) => i % 2 == 0).ToList();
        var v2Documents = documents.Where((_, i) => i % 2 == 1).ToList();

        Assert.All(v2Documents, AssertNoEnumExtensions);
        Assert.All(v1Documents, document =>
        {
            var schema = Schemas(document)["OrderStatus"]!;
            var count = schema["enum"]!.AsArray().Count;

            Assert.Equal(3, count);
            Assert.Equal(count, schema["x-enum-varnames"]!.AsArray().Count);
            Assert.Equal(count, schema["x-enumNames"]!.AsArray().Count);
        });
    }

    private static Task<WebApplication> StartTwoDocuments()
    {
        Action<OpenApiOptions> register = options => options.AddEnumDescriptions();

        return TestApi.Start<Order>(("v1", register), ("v2", _ => { }));
    }

    private static JsonObject Schemas(JsonNode document)
    {
        return document["components"]!["schemas"]!.AsObject();
    }

    private static void AssertNoEnumExtensions(JsonNode document)
    {
        Assert.DoesNotContain("\"x-enum", document.ToJsonString(), StringComparison.Ordinal);
    }
}
