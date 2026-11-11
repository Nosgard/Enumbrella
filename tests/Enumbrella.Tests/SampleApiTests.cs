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

    [Fact]
    public async Task AddEnumDescriptions_SampleApi_PaymentMethodListsStringsNamesAndDescriptions()
    {
        // Given: the sample API, which writes PaymentMethod as camelCase strings

        // When: the document is generated
        var document = await SampleApi.Document();

        // Then: each string has its C# name and its description at the same position
        var schema = document["components"]!["schemas"]!["PaymentMethod"]!;
        var descriptions = "[\"Paid by card\",\"Paid on invoice\",\"Paid by bank transfer\"]";

        Assert.Equal("[\"card\",\"invoice\",\"bankTransfer\"]", schema["enum"]!.ToJsonString());
        Assert.Equal(
            "[\"Card\",\"Invoice\",\"BankTransfer\"]", schema["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(descriptions, schema["x-enum-descriptions"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_SampleApi_OrderStatusCarriesDescriptions()
    {
        // Given: the sample API with descriptions on Pending and Approved

        // When: the document is generated
        var document = await SampleApi.Document();

        // Then: OrderStatus lists both descriptions and an empty one for Rejected
        var schema = document["components"]!["schemas"]!["OrderStatus"]!;

        Assert.Equal(
            "[\"Waiting for approval\",\"Approved\",\"\"]",
            schema["x-enum-descriptions"]!.ToJsonString());
    }
}
