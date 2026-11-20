namespace Enumbrella.Tests;

public class ValueOrderTests
{
    [Fact]
    public async Task AddEnumDescriptions_UnsortedEnum_YieldsAscendingValuesWithNamesOnly()
    {
        // Given: an enum declared out of value order
        await using var host = await TestApi.Start<PriorityResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the values ascend and each name stays with its value
        TestApi.AssertEnum(
            TestApi.Schema(document, "Priority"),
            "[1,5,10]",
            "[\"Low\",\"Medium\",\"High\"]",
            null);
    }

    [Fact]
    public async Task AddEnumDescriptions_NegativeValues_ComeFirstWithMatchingNames()
    {
        // Given: an enum with negative values
        await using var host = await TestApi.Start<TemperatureResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the lowest value comes first and each name stays with it
        TestApi.AssertEnum(
            TestApi.Schema(document, "Temperature"),
            "[-10,-1,0,5]",
            "[\"Freezing\",\"Cold\",\"Mild\",\"Warm\"]",
            null);
    }

    [Fact]
    public async Task AddEnumDescriptions_ByteEnum_YieldsItsValuesWithMatchingNames()
    {
        // Given: an enum based on byte
        await using var host = await TestApi.Start<LevelResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the values reach 255 and each name stays with its value
        TestApi.AssertEnum(
            TestApi.Schema(document, "Level"),
            "[1,255]",
            "[\"Low\",\"High\"]",
            null);
    }

    [Fact]
    public async Task AddEnumDescriptions_LongEnum_YieldsValuesBeyondIntRangeWithMatchingNames()
    {
        // Given: an enum based on long with a value beyond the int range
        await using var host = await TestApi.Start<DistanceResponse>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the large value stays intact and keeps its name
        TestApi.AssertEnum(
            TestApi.Schema(document, "Distance"),
            "[1,10000000000]",
            "[\"Near\",\"Far\"]",
            null);
    }
}
