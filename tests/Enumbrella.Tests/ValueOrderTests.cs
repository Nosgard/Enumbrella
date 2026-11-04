using System.Text.Json.Nodes;

namespace Enumbrella.Tests;

public class ValueOrderTests
{
    [Fact]
    public async Task unsorted_declaration_yields_values_ascending_with_matching_names()
    {
        var schema = await Schema<PriorityResponse>("Priority");

        Assert.Equal("[1,5,10]", schema["enum"]!.ToJsonString());
        Assert.Equal("[\"Low\",\"Medium\",\"High\"]", schema["x-enum-varnames"]!.ToJsonString());
    }

    [Fact]
    public async Task negative_values_come_first_with_matching_names()
    {
        var schema = await Schema<TemperatureResponse>("Temperature");

        Assert.Equal("[-10,-1,0,5]", schema["enum"]!.ToJsonString());
        Assert.Equal(
            "[\"Freezing\",\"Cold\",\"Mild\",\"Warm\"]", schema["x-enum-varnames"]!.ToJsonString());
    }

    [Fact]
    public async Task byte_enum_yields_its_values_with_matching_names()
    {
        var schema = await Schema<LevelResponse>("Level");

        Assert.Equal("[1,255]", schema["enum"]!.ToJsonString());
        Assert.Equal("[\"Low\",\"High\"]", schema["x-enum-varnames"]!.ToJsonString());
    }

    [Fact]
    public async Task long_enum_yields_values_beyond_int_range_with_matching_names()
    {
        var schema = await Schema<DistanceResponse>("Distance");

        Assert.Equal("[1,10000000000]", schema["enum"]!.ToJsonString());
        Assert.Equal("[\"Near\",\"Far\"]", schema["x-enum-varnames"]!.ToJsonString());
    }

    private static async Task<JsonNode> Schema<TResponse>(string name)
    {
        var document = await TestApi.Document<TResponse>(options => options.AddEnumDescriptions());

        return document["components"]!["schemas"]![name]!;
    }
}
