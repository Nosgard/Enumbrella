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

    [Fact]
    public async Task x_enumNames_equals_x_enum_varnames_on_every_schema_that_has_it()
    {
        var document = await SampleApi.Document();

        var schemas = document["components"]!["schemas"]!.AsObject();
        var withVarNames = schemas.Where(s => s.Value!["x-enum-varnames"] is not null).ToList();

        Assert.NotEmpty(withVarNames);
        Assert.All(withVarNames, s => Assert.Equal(
            s.Value!["x-enum-varnames"]!.ToJsonString(),
            s.Value!["x-enumNames"]!.ToJsonString()));
    }
}
