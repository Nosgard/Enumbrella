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

        var schemas = document["components"]!["schemas"]!.AsObject();
        var schema = schemas["OrderStatus"]!;
        var varNames = schema["x-enum-varnames"]!.AsArray().Select(n => n!.GetValue<string>());
        var enumNames = schema["x-enumNames"]!.AsArray().Select(n => n!.GetValue<string>());

        Assert.Single(schemas, s => s.Key.Contains("OrderStatus", StringComparison.Ordinal));
        Assert.Equal("[0,1,2]", schema["enum"]!.ToJsonString());
        Assert.Equal(["Pending", "Approved", "Rejected"], varNames);
        Assert.Equal(["Pending", "Approved", "Rejected"], enumNames);
    }
}
