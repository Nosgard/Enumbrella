using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Enumbrella.Tests;

internal static class SampleApi
{
    // Starts the host here, so the first client of a test fetches from it instead of building it.
    public static WebApplicationFactory<Program> Start()
    {
        return StartServer(new WebApplicationFactory<Program>());
    }

    // Sets the OpenAPI version of document v1. The sample sets none.
    public static WebApplicationFactory<Program> Start(OpenApiSpecVersion version)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(
            host => host.ConfigureServices(services => services.Configure<OpenApiOptions>(
                "v1", options => options.OpenApiVersion = version)));

        return StartServer(factory);
    }

    public static async Task<JsonNode> Fetch(WebApplicationFactory<Program> sampleApi)
    {
        using var client = sampleApi.CreateClient();

        return await TestApi.Fetch(client);
    }

    private static WebApplicationFactory<Program> StartServer(
        WebApplicationFactory<Program> factory)
    {
        factory.StartServer();

        return factory;
    }
}
