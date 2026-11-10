using System.Text.Json.Nodes;
using Enumbrella.Sample;
using Microsoft.AspNetCore.OpenApi;

namespace Enumbrella.Tests;

public class ListLengthTests
{
    private static readonly Action<OpenApiOptions> Register =
        options => options.AddEnumDescriptions();

    [Fact]
    public async Task every_enum_schema_has_equal_name_lists_as_long_as_enum()
    {
        // Given: the documents of the sample API and of each test API

        // When: the schemas with name lists are collected
        var enumSchemas = EnumSchemas(await Documents());

        // Then: each name list is as long as enum and both name lists match
        Assert.Equal(18, enumSchemas.Count);
        Assert.All(enumSchemas, schema =>
        {
            var count = schema["enum"]!.AsArray().Count;

            Assert.Equal(count, schema["x-enum-varnames"]!.AsArray().Count);
            Assert.Equal(
                schema["x-enum-varnames"]!.ToJsonString(), schema["x-enumNames"]!.ToJsonString());
        });
    }

    private static async Task<JsonNode[]> Documents()
    {
        return
        [
            await SampleApi.Document(),
            await TestApi.Document<Order>(Register),
            await TestApi.Document<PriorityResponse>(Register),
            await TestApi.Document<TemperatureResponse>(Register),
            await TestApi.Document<LevelResponse>(Register),
            await TestApi.Document<DistanceResponse>(Register),
            await TestApi.Document<NullableStatusResponse>(Register),
            await TestApi.InlineEnumDocument<NullableStatusResponse>(Register),
            await TestApi.InlineEnumDocument<StringPropertyResponse>(Register),
            await TestApi.Document<ScopeResponse>(Register),
            await TestApi.Document<TwoStatusesResponse>(Register),
            await TestApi.Document<NullableFirstResponse>(Register),
            await TestApi.Document<PlainFirstResponse>(Register),
            await TestApi.Document<StringFirstResponse>(Register),
            await TestApi.Document<NumberFirstResponse>(Register),
            await TestApi.EndpointDocument(TestApi.MapNullableStatus, TestApi.MapPlainStatus),
            await TestApi.EndpointDocument(TestApi.MapPlainStatus, TestApi.MapNullableStatus),
        ];
    }

    private static List<JsonNode> EnumSchemas(JsonNode[] documents)
    {
        var components = documents
            .SelectMany(document => document["components"]!["schemas"]!.AsObject())
            .Select(schema => schema.Value!)
            .ToList();
        var properties = components
            .SelectMany(schema => schema["properties"]?.AsObject() ?? [])
            .Select(property => property.Value!);

        return components
            .Concat(properties)
            .Where(schema => schema["x-enum-varnames"] is not null)
            .ToList();
    }
}
