using Enumbrella.Sample;

namespace Enumbrella.Tests;

public class ConverterFailureTests
{
    [Fact]
    public async Task AddEnumDescriptions_ForeignUnreadableEntry_GetsEmptyNameAndDescription()
    {
        // Given: a schema transformer that writes the unreadable "unknown" and the readable 0
        // before the registration
        await using var host = await TestApi.Start<Order>(
            TestApi.RegisterAfterEntries<OrderStatus>("[\"unknown\",0]"));

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the unreadable entry gets an empty name and description, the readable one its own
        TestApi.AssertEnum(
            TestApi.Schema(document, "OrderStatus"),
            "[\"unknown\",0]",
            "[\"\",\"Pending\"]",
            "[\"\",\"Waiting for approval\"]");
    }

    [Fact]
    public async Task AddEnumDescriptions_ForeignUndeclaredValue_GetsEmptyNameAndDescription()
    {
        // Given: a schema transformer that writes 0, the undeclared 7 and 1 before the registration
        await using var host =
            await TestApi.Start<Order>(TestApi.RegisterAfterEntries<OrderStatus>("[0,7,1]"));

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: 7 gets an empty name and description, and 0 and 1 keep their own at their position
        TestApi.AssertEnum(
            TestApi.Schema(document, "OrderStatus"),
            "[0,7,1]",
            "[\"Pending\",\"\",\"Approved\"]",
            "[\"Waiting for approval\",\"\",\"Approved\"]");
    }
}
