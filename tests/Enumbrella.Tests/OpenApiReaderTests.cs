namespace Enumbrella.Tests;

public class OpenApiReaderTests
{
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
