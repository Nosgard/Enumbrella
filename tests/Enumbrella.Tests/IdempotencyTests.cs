using Enumbrella.Sample;

namespace Enumbrella.Tests;

public class IdempotencyTests
{
    [Fact]
    public async Task registering_twice_yields_the_same_document()
    {
        var once = await TestApi.Document<Order>(options => options.AddEnumDescriptions());
        var twice = await TestApi.Document<Order>(
            options => options.AddEnumDescriptions().AddEnumDescriptions());

        Assert.Equal(once.ToJsonString(), twice.ToJsonString());
    }

    [Fact]
    public async Task an_enum_used_twice_yields_one_component_without_duplicate_names()
    {
        var document = await TestApi.Document<TwoStatusesResponse>(
            options => options.AddEnumDescriptions());

        const string names = "[\"Pending\",\"Approved\",\"Rejected\"]";
        var schemas = document["components"]!["schemas"]!.AsObject();
        var schema = schemas["OrderStatus"]!;

        Assert.Single(schemas, s => s.Key.Contains("OrderStatus", StringComparison.Ordinal));
        Assert.Equal("[0,1,2]", schema["enum"]!.ToJsonString());
        Assert.Equal(names, schema["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, schema["x-enumNames"]!.ToJsonString());
    }
}
