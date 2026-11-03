using System.Text.Json.Nodes;
using Enumbrella.Sample;

namespace Enumbrella.Tests;

public class ListLengthTests
{
    [Fact]
    public async Task every_enum_schema_has_name_lists_as_long_as_enum()
    {
        JsonNode[] documents =
        [
            await SampleApi.Document(),
            await TestApi.Document<Order>(options => options.AddEnumDescriptions()),
            await TestApi.Document<PriorityResponse>(options => options.AddEnumDescriptions()),
            await TestApi.Document<TemperatureResponse>(options => options.AddEnumDescriptions()),
            await TestApi.Document<LevelResponse>(options => options.AddEnumDescriptions()),
            await TestApi.Document<DistanceResponse>(options => options.AddEnumDescriptions()),
            await TestApi.Document<ConverterFailureTests.UnreadableResponse>(
                options => options.AddEnumDescriptions()),
        ];

        var enumSchemas = documents
            .SelectMany(document => document["components"]!["schemas"]!.AsObject())
            .Select(schema => schema.Value!)
            .Where(schema => schema["x-enum-varnames"] is not null)
            .ToList();

        Assert.Equal(8, enumSchemas.Count);
        Assert.All(enumSchemas, schema =>
        {
            var count = schema["enum"]!.AsArray().Count;

            Assert.Equal(count, schema["x-enum-varnames"]!.AsArray().Count);
            Assert.Equal(count, schema["x-enumNames"]!.AsArray().Count);
        });
    }
}
