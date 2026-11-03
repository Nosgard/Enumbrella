using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Enumbrella.Tests;

/// <summary>Fetches the OpenAPI document <c>v1</c> of the sample API hosted in the test process.</summary>
internal static class SampleApi
{
    public static async Task<JsonNode> Document()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var json = await client.GetStringAsync("/openapi/v1.json");

        return JsonNode.Parse(json)!;
    }
}
