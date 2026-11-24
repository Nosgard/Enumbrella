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
