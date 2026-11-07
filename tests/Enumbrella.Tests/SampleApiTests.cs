namespace Enumbrella.Tests;

public class SampleApiTests
{
    [Fact]
    public async Task OrderStatus_schema_lists_values_and_both_name_lists()
    {
        const string names = "[\"Pending\",\"Approved\",\"Rejected\"]";
        var document = await SampleApi.Document();

        var schema = document["components"]!["schemas"]!["OrderStatus"]!;

        Assert.Equal("[0,1,2]", schema["enum"]!.ToJsonString());
        Assert.Equal(names, schema["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, schema["x-enumNames"]!.ToJsonString());
    }
}
