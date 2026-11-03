using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Enumbrella.Tests;

/// <summary>
/// Builds a minimal API in the test process whose only endpoint returns
/// <typeparamref name="TResponse"/> and fetches its document <c>v1</c> over the test server.
/// </summary>
internal static class TestApi
{
    public static async Task<JsonNode> Document<TResponse>(Action<OpenApiOptions> configure)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddOpenApi(configure);

        await using var app = builder.Build();
        app.MapOpenApi();
        app.MapGet("/", () => default(TResponse)!);
        await app.StartAsync();

        using var client = app.GetTestClient();
        var json = await client.GetStringAsync("/openapi/v1.json");

        return JsonNode.Parse(json)!;
    }
}
