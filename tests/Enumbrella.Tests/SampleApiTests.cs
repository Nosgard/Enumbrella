using System.Text.Json.Nodes;

namespace Enumbrella.Tests;

public class SampleApiTests
{
    [Fact]
    public async Task OrderStatus_schema_lists_values_and_both_name_lists()
    {
        var document = await SampleApi.Document();

        var schema = document["components"]!["schemas"]!["OrderStatus"]!;

        Assert.Equal("[0,1,2]", schema["enum"]!.ToJsonString());
        Assert.Equal("[\"Pending\",\"Approved\",\"Rejected\"]", Names(schema, "x-enum-varnames"));
        Assert.Equal("[\"Pending\",\"Approved\",\"Rejected\"]", Names(schema, "x-enumNames"));
    }

    [Fact]
    public async Task x_enumNames_equals_x_enum_varnames_on_every_schema_that_has_it()
    {
        var document = await SampleApi.Document();

        var schemas = document["components"]!["schemas"]!.AsObject();
        var withVarNames = schemas.Where(s => s.Value!["x-enum-varnames"] is not null).ToList();

        Assert.NotEmpty(withVarNames);
        Assert.All(withVarNames, s => Assert.Equal(
            Names(s.Value!, "x-enum-varnames"), Names(s.Value!, "x-enumNames")));
    }

    private static string Names(JsonNode schema, string extension)
    {
        return schema[extension]!.ToJsonString();
    }
}
