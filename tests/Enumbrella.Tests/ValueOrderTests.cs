using System.Text.Json.Nodes;

namespace Enumbrella.Tests;

public class ValueOrderTests
{
    [Fact]
    public async Task AddEnumDescriptions_UnsortedEnum_YieldsAscendingValuesWithMatchingNames()
    {
        // Given: an enum declared out of value order

        // When: the document is generated
        var schema = await Schema<PriorityResponse>("Priority");

        // Then: the values ascend and each name stays with its value
        Assert.Equal("[1,5,10]", schema["enum"]!.ToJsonString());
        Assert.Equal("[\"Low\",\"Medium\",\"High\"]", schema["x-enum-varnames"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_NegativeValues_ComeFirstWithMatchingNames()
    {
        // Given: an enum with negative values

        // When: the document is generated
        var schema = await Schema<TemperatureResponse>("Temperature");

        // Then: the lowest value comes first and each name stays with it
        Assert.Equal("[-10,-1,0,5]", schema["enum"]!.ToJsonString());
        Assert.Equal(
            "[\"Freezing\",\"Cold\",\"Mild\",\"Warm\"]", schema["x-enum-varnames"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_ByteEnum_YieldsItsValuesWithMatchingNames()
    {
        // Given: an enum based on byte

        // When: the document is generated
        var schema = await Schema<LevelResponse>("Level");

        // Then: the values reach 255 and each name stays with its value
        Assert.Equal("[1,255]", schema["enum"]!.ToJsonString());
        Assert.Equal("[\"Low\",\"High\"]", schema["x-enum-varnames"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_LongEnum_YieldsValuesBeyondIntRangeWithMatchingNames()
    {
        // Given: an enum based on long with a value beyond the int range

        // When: the document is generated
        var schema = await Schema<DistanceResponse>("Distance");

        // Then: the large value stays intact and keeps its name
        Assert.Equal("[1,10000000000]", schema["enum"]!.ToJsonString());
        Assert.Equal("[\"Near\",\"Far\"]", schema["x-enum-varnames"]!.ToJsonString());
    }

    private static async Task<JsonNode> Schema<TResponse>(string name)
    {
        var document = await TestApi.Document<TResponse>(options => options.AddEnumDescriptions());

        return document["components"]!["schemas"]![name]!;
    }
}
