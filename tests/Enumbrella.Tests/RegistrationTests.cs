using Enumbrella.Sample;

namespace Enumbrella.Tests;

public class RegistrationTests
{
    [Fact]
    public async Task schema_of_OrderStatus_has_no_enum_without_registration()
    {
        var document = await TestApi.Document<Order>(_ => { });

        var schema = document["components"]!["schemas"]!["OrderStatus"]!;

        Assert.Null(schema["enum"]);
    }
}
