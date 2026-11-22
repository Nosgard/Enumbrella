using Enumbrella.Sample;

namespace Enumbrella.Tests;

public class LocationTests
{
    [Fact]
    public async Task AddEnumDescriptions_StatusAtEveryLocation_SharesOneListedComponent()
    {
        // Given: OrderStatus as property, path parameter, query parameter and body element
        await using var host = await TestApi.StartLocations<Order, OrderStatus>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: each location refers to the one OrderStatus component, which lists each value once
        var reference = "{\"$ref\":\"#/components/schemas/OrderStatus\"}";

        Assert.Equal(reference, TestApi.Property(document, "Order", "status").ToJsonString());
        Assert.All(
            TestApi.LocationSchemas(document),
            schema => Assert.Equal(reference, schema.ToJsonString()));
        TestApi.AssertOneStatusComponent(document);
    }

    [Fact]
    public async Task AddEnumDescriptions_InlineStatusAtEveryLocation_ListsAtEachLocation()
    {
        // Given: OrderStatus inline as property, path parameter, query parameter and body element
        await using var host =
            await TestApi.StartInlineLocations<Order, OrderStatus>(TestApi.Register);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: each location carries the values, both name lists and the descriptions
        var values = "[0,1,2]";
        var names = "[\"Pending\",\"Approved\",\"Rejected\"]";
        var descriptions = "[\"Waiting for approval\",\"Approved\",\"\"]";

        TestApi.AssertEnum(
            TestApi.Property(document, "Order", "status"), values, names, descriptions);
        Assert.All(
            TestApi.LocationSchemas(document),
            schema => TestApi.AssertEnum(schema, values, names, descriptions));
    }
}
