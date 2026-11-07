using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Enumbrella.Tests;

internal static class TestApi
{
    public static async Task<JsonNode> Document<TResponse>(Action<OpenApiOptions> configure)
    {
        await using var app = await Start(MapResponse<TResponse>, ("v1", configure));

        return await Fetch(app, "v1");
    }

    public static async Task<WebApplication> Start(
        Action<WebApplication> mapEndpoints,
        params (string Name, Action<OpenApiOptions> Configure)[] documents)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        foreach (var (name, configure) in documents)
        {
            builder.Services.AddOpenApi(name, configure);
        }

        var app = builder.Build();
        app.MapOpenApi();
        mapEndpoints(app);
        await app.StartAsync();

        return app;
    }

    public static async Task<JsonNode> Fetch(WebApplication app, string document)
    {
        using var client = app.GetTestClient();
        var json = await client.GetStringAsync($"/openapi/{document}.json");

        return JsonNode.Parse(json)!;
    }

    public static void MapResponse<TResponse>(WebApplication app)
    {
        app.MapGet("/", () => default(TResponse)!);
    }

    public static Task<JsonNode> InlineEnumDocument<TResponse>(Action<OpenApiOptions> configure)
    {
        return Document<TResponse>(options =>
        {
            options.CreateSchemaReferenceId = InlineEnumReferenceId;
            configure(options);
        });
    }

    public static void AssertStatusComponent(JsonNode document)
    {
        var schema = document["components"]!["schemas"]!["OrderStatus"]!;
        var names = "[\"Pending\",\"Approved\",\"Rejected\"]";

        Assert.Equal("[0,1,2]", schema["enum"]!.ToJsonString());
        Assert.Equal(names, schema["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, schema["x-enumNames"]!.ToJsonString());
    }

    private static string? InlineEnumReferenceId(JsonTypeInfo typeInfo)
    {
        var type = Nullable.GetUnderlyingType(typeInfo.Type) ?? typeInfo.Type;

        return type.IsEnum ? null : OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
    }
}
