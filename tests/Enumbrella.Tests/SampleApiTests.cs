namespace Enumbrella.Tests;

public class SampleApiTests
{
    [Fact]
    public async Task OrderStatus_schema_lists_values_and_both_name_lists()
    {
        // Given: the sample API with its registration

        // When: the document is generated
        var document = await SampleApi.Document();

        // Then: OrderStatus lists its values and both name lists
        TestApi.AssertStatusComponent(document);
    }
}
