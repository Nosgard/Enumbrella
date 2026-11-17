namespace Enumbrella.Tests;

public class SampleApiTests
{
    [Fact]
    public async Task AddEnumDescriptions_SampleApi_OrderStatusListsValuesNamesAndDescriptions()
    {
        // Given: the sample API with its registration
        await using var sampleApi = SampleApi.Start();

        // When: the document is generated
        var document = await SampleApi.Fetch(sampleApi);

        // Then: OrderStatus lists its values, both name lists and its descriptions
        TestApi.AssertStatusComponent(document);
    }

    [Fact]
    public async Task AddEnumDescriptions_SampleApi_PaymentMethodListsStringsNamesAndDescriptions()
    {
        // Given: the sample API, which writes PaymentMethod as camelCase strings
        await using var sampleApi = SampleApi.Start();

        // When: the document is generated
        var document = await SampleApi.Fetch(sampleApi);

        // Then: each string has its C# name and its description at the same position
        TestApi.AssertEnum(
            TestApi.Schema(document, "PaymentMethod"),
            "[\"card\",\"invoice\",\"bankTransfer\"]",
            "[\"Card\",\"Invoice\",\"BankTransfer\"]",
            "[\"Paid by card\",\"Paid on invoice\",\"Paid by bank transfer\"]");
    }
}
