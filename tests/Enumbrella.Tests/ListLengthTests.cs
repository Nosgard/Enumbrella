using System.Text.Json.Nodes;
using Enumbrella.Sample;

namespace Enumbrella.Tests;

public class ListLengthTests
{
    [Fact]
    public async Task every_enum_schema_has_equal_name_lists_as_long_as_enum()
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
            await TestApi.Document<ConverterFailureTests.UndeclaredResponse>(
                options => options.AddEnumDescriptions()),
            await TestApi.Document<NullableStatusResponse>(
                options => options.AddEnumDescriptions()),
            await TestApi.InlineEnumDocument<NullableStatusResponse>(
                options => options.AddEnumDescriptions()),
        ];

        var components = documents
            .SelectMany(document => document["components"]!["schemas"]!.AsObject())
            .Select(schema => schema.Value!)
            .ToList();
        var properties = components
            .SelectMany(schema => schema["properties"]?.AsObject() ?? [])
            .Select(property => property.Value!);
        var enumSchemas = components
            .Concat(properties)
            .Where(schema => schema["x-enum-varnames"] is not null)
            .ToList();

        Assert.Equal(11, enumSchemas.Count);
        Assert.All(enumSchemas, schema =>
        {
            var count = schema["enum"]!.AsArray().Count;

            Assert.Equal(count, schema["x-enum-varnames"]!.AsArray().Count);
            Assert.Equal(
                schema["x-enum-varnames"]!.ToJsonString(), schema["x-enumNames"]!.ToJsonString());
        });
    }
}
