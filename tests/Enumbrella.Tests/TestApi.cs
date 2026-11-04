using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Enumbrella.Tests;

internal static class TestApi
{
    public static async Task<JsonNode> Document<TResponse>(Action<OpenApiOptions> configure)
    {
        await using var app = await Start<TResponse>(("v1", configure));

        return await Fetch(app, "v1");
    }

    public static async Task<WebApplication> Start<TResponse>(
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
        app.MapGet("/", () => default(TResponse)!);
        await app.StartAsync();

        return app;
    }

    public static async Task<JsonNode> Fetch(WebApplication app, string document)
    {
        using var client = app.GetTestClient();
        var json = await client.GetStringAsync($"/openapi/{document}.json");

        return JsonNode.Parse(json)!;
    }
}
