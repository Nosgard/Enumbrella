namespace Enumbrella.Tests;

public class SampleApiTests
{
    [Fact]
    public async Task AddEnumDescriptions_SampleApi_OrderStatusSchemaListsValuesAndBothNameLists()
    {
        // Given: the sample API with its registration

        // When: the document is generated
        var document = await SampleApi.Document();

        // Then: OrderStatus lists its values and both name lists
        TestApi.AssertStatusComponent(document);
    }
}
