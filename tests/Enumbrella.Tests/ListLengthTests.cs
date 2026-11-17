using System.Text.Json.Nodes;
using Enumbrella.Sample;

namespace Enumbrella.Tests;

public class ListLengthTests
{
    [Fact]
    public async Task AddEnumDescriptions_EveryEnumSchema_HasEveryListAsLongAsEnum()
    {
        // Given: the documents of the sample API and of each test API
        var documents = await Documents();

        // When: the schemas with name lists are collected
        var enumSchemas = EnumSchemas(documents);

        // Then: each name and description list is as long as enum and both name lists match
        Assert.Equal(27, enumSchemas.Count);
        Assert.All(enumSchemas, schema =>
        {
            var count = schema["enum"]!.AsArray().Count;

            Assert.Equal(count, schema["x-enum-varnames"]!.AsArray().Count);
            Assert.Equal(
                schema["x-enum-varnames"]!.ToJsonString(), schema["x-enumNames"]!.ToJsonString());

            if (schema["x-enum-descriptions"] is JsonArray descriptions)
            {
                Assert.Equal(count, descriptions.Count);
            }
        });
    }

    private static async Task<JsonNode[]> Documents()
    {
        return
        [
            .. await StandardDocuments(),
            .. await DescriptionDocuments(),
            .. await SpecialHostDocuments(),
        ];
    }

    private static async Task<JsonNode[]> StandardDocuments()
    {
        return
        [
            await SampleApi.Document(),
            await TestApi.Document<Order>(TestApi.Register),
            await TestApi.Document<PriorityResponse>(TestApi.Register),
            await TestApi.Document<TemperatureResponse>(TestApi.Register),
            await TestApi.Document<LevelResponse>(TestApi.Register),
            await TestApi.Document<DistanceResponse>(TestApi.Register),
            await TestApi.Document<NullableStatusResponse>(TestApi.Register),
            await TestApi.Document<ScopeResponse>(TestApi.Register),
            await TestApi.Document<TwoStatusesResponse>(TestApi.Register),
            await TestApi.Document<NullableFirstResponse>(TestApi.Register),
            await TestApi.Document<PlainFirstResponse>(TestApi.Register),
            await TestApi.Document<StringFirstResponse>(TestApi.Register),
            await TestApi.Document<NumberFirstResponse>(TestApi.Register),
        ];
    }

    private static async Task<JsonNode[]> DescriptionDocuments()
    {
        return
        [
            await TestApi.Document<Order>(TestApi.Register),
            await TestApi.Document<SpecialTextResponse>(TestApi.Register),
            await TestApi.Document<SwitchResponse>(TestApi.Register),
            await TestApi.Document<ShippingResponse>(TestApi.Register),
            await TestApi.Document<ChannelResponse>(TestApi.Register),
            await TestApi.Document<LabelledResponse>(TestApi.Register),
            await TestApi.Document<LocalizedResponse>(TestApi.Register),
        ];
    }

    private static async Task<JsonNode[]> SpecialHostDocuments()
    {
        return
        [
            await TestApi.Document<Order>(options => options
                .AddSchemaTransformer(TestApi.WriteUnreadableStatusEntry)
                .AddEnumDescriptions()),
            await TestApi.InlineEnumDocument<NullableStatusResponse>(TestApi.Register),
            await TestApi.InlineEnumDocument<StringPropertyResponse>(TestApi.Register),
            await TestApi.InlineEnumDocument<NullableStringPropertyResponse>(TestApi.Register),
            await TestApi.EndpointDocument(TestApi.MapNullableStatus, TestApi.MapPlainStatus),
            await TestApi.EndpointDocument(TestApi.MapPlainStatus, TestApi.MapNullableStatus),
        ];
    }

    private static List<JsonNode> EnumSchemas(JsonNode[] documents)
    {
        var components = documents
            .SelectMany(document => TestApi.Schemas(document))
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
