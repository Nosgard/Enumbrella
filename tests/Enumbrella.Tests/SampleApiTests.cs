namespace Enumbrella.Tests;

public class SampleApiTests
{
    [Fact]
    public async Task OrderStatus_schema_lists_values_and_both_name_lists()
    {
        var document = await SampleApi.Document();

        TestApi.AssertStatusComponent(document);
    }
}
