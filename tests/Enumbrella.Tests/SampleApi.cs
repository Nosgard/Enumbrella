using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Enumbrella.Tests;

internal static class SampleApi
{
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

        return await TestApi.Fetch(client);
    }
}
