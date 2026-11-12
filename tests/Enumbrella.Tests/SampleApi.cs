using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Enumbrella.Tests;

internal static class SampleApi
{
    // For tests whose starting state is the finished document, such as the list length test.
    public static async Task<JsonNode> Document()
    {
        await using var sampleApi = Start();

        return await Fetch(sampleApi);
    }

    // Starts the host here, so the first client of a test fetches from it instead of building it.
    public static WebApplicationFactory<Program> Start()
    {
        var factory = new WebApplicationFactory<Program>();
        factory.StartServer();

        return factory;
    }

    public static async Task<JsonNode> Fetch(WebApplicationFactory<Program> sampleApi)
    {
        using var client = sampleApi.CreateClient();
        var json = await client.GetStringAsync("/openapi/v1.json");

        return JsonNode.Parse(json)!;
    }
}
