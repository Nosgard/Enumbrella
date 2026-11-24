using Microsoft.OpenApi;

namespace Enumbrella.Tests;

public class OpenApiReaderTests
{
    [Fact]
    public async Task AddEnumDescriptions_SampleApi_ReadsAsValidOpenApi31WithListExtensions()
    {
        // Given: the sample API, which sets no OpenAPI version
        await using var sampleApi = SampleApi.Start();

        // When: the document is generated
        var document = await SampleApi.Fetch(sampleApi);

        // Then: the reader finds no errors in the 3.1 document and keeps the lists as extensions
        var result = TestApi.Read(document.ToJsonString());

        Assert.StartsWith("3.1.", (string?)document["openapi"], StringComparison.Ordinal);
        Assert.Empty(result.Diagnostic!.Errors);
        TestApi.AssertListExtensions(result.Document!, "OrderStatus", "PaymentMethod");
    }

    [Fact]
    public async Task AddEnumDescriptions_SampleApiSetTo30_ReadsAsValidOpenApi30WithListExtensions()
    {
        // Given: the sample API with its OpenAPI version set to 3.0
        await using var sampleApi = SampleApi.Start(OpenApiSpecVersion.OpenApi3_0);

        // When: the document is generated
        var document = await SampleApi.Fetch(sampleApi);

        // Then: the reader finds no errors in the 3.0 document and keeps the lists as extensions,
        // and both components list their values, names and descriptions
        var result = TestApi.Read(document.ToJsonString());

        Assert.StartsWith("3.0.", (string?)document["openapi"], StringComparison.Ordinal);
        Assert.Empty(result.Diagnostic!.Errors);
        TestApi.AssertListExtensions(result.Document!, "OrderStatus", "PaymentMethod");
        TestApi.AssertStatusComponent(document);
        TestApi.AssertEnum(
            TestApi.Schema(document, "PaymentMethod"),
            "[\"card\",\"invoice\",\"bankTransfer\"]",
            "[\"Card\",\"Invoice\",\"BankTransfer\"]",
            "[\"Paid by card\",\"Paid on invoice\",\"Paid by bank transfer\"]");
    }

    [Fact]
    public void Parse_DocumentWithoutInfo_ReportsErrors()
    {
        // Given: a 3.1 document without info
        const string document = """{"openapi":"3.1.1","paths":{}}""";

        // When: the reader reads it
        var result = TestApi.Read(document);

        // Then: it reports at least one error
        Assert.NotEmpty(result.Diagnostic!.Errors);
    }
}
