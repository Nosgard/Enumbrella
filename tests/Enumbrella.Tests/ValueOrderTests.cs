using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;

namespace Enumbrella.Tests;

public class ValueOrderTests
{
    private static readonly Action<OpenApiOptions> Register =
        options => options.AddEnumDescriptions();

    [Fact]
    public async Task AddEnumDescriptions_UnsortedEnum_YieldsAscendingValuesWithMatchingNames()
    {
        // Given: an enum declared out of value order
        await using var host = await TestApi.Start<PriorityResponse>(Register);

        // When: the document is generated
        var schema = await Schema(host, "Priority");

        // Then: the values ascend and each name stays with its value
        Assert.Equal("[1,5,10]", schema["enum"]!.ToJsonString());
        Assert.Equal("[\"Low\",\"Medium\",\"High\"]", schema["x-enum-varnames"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_NegativeValues_ComeFirstWithMatchingNames()
    {
        // Given: an enum with negative values
        await using var host = await TestApi.Start<TemperatureResponse>(Register);

        // When: the document is generated
        var schema = await Schema(host, "Temperature");

        // Then: the lowest value comes first and each name stays with it
        Assert.Equal("[-10,-1,0,5]", schema["enum"]!.ToJsonString());
        Assert.Equal(
            "[\"Freezing\",\"Cold\",\"Mild\",\"Warm\"]", schema["x-enum-varnames"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_ByteEnum_YieldsItsValuesWithMatchingNames()
    {
        // Given: an enum based on byte
        await using var host = await TestApi.Start<LevelResponse>(Register);

        // When: the document is generated
        var schema = await Schema(host, "Level");

        // Then: the values reach 255 and each name stays with its value
        Assert.Equal("[1,255]", schema["enum"]!.ToJsonString());
        Assert.Equal("[\"Low\",\"High\"]", schema["x-enum-varnames"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_LongEnum_YieldsValuesBeyondIntRangeWithMatchingNames()
    {
        // Given: an enum based on long with a value beyond the int range
        await using var host = await TestApi.Start<DistanceResponse>(Register);

        // When: the document is generated
        var schema = await Schema(host, "Distance");

        // Then: the large value stays intact and keeps its name
        Assert.Equal("[1,10000000000]", schema["enum"]!.ToJsonString());
        Assert.Equal("[\"Near\",\"Far\"]", schema["x-enum-varnames"]!.ToJsonString());
    }

    private static async Task<JsonNode> Schema(WebApplication host, string name)
    {
        var document = await TestApi.Fetch(host);

        return document["components"]!["schemas"]![name]!;
    }
}
