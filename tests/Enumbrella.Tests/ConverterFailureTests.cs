using System.Text.Json;
using System.Text.Json.Serialization;
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

    [Fact]
    public async Task AddEnumDescriptions_ForeignTypeOnUnsupported_MatchesUnregisteredSchema()
    {
        // Given: a schema transformer that sets the string type on Unsupported, whose converter
        // throws NotSupportedException, in one host with the registration after it and one without
        await using var registeredHost = await TestApi.Start<UnsupportedResponse>(
            TestApi.WithStringType<Unsupported>(TestApi.Register));
        await using var unregisteredHost =
            await TestApi.Start<UnsupportedResponse>(TestApi.WithStringType<Unsupported>(_ => { }));

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: without the registration the Unsupported schema holds the foreign string type,
        // and with it the schema is the same
        var unregisteredSchema = TestApi.Schema(unregistered, "Unsupported").ToJsonString();

        Assert.Equal("{\"type\":\"string\"}", unregisteredSchema);
        Assert.Equal(unregisteredSchema, TestApi.Schema(registered, "Unsupported").ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_ForeignTypeOnMalformed_MatchesUnregisteredSchema()
    {
        // Given: a schema transformer that sets the string type on Malformed, whose converter
        // throws JsonException, in one host with the registration after it and one without
        await using var registeredHost = await TestApi.Start<MalformedResponse>(
            TestApi.WithStringType<Malformed>(TestApi.Register));
        await using var unregisteredHost =
            await TestApi.Start<MalformedResponse>(TestApi.WithStringType<Malformed>(_ => { }));

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: without the registration the Malformed schema holds the foreign string type, and
        // with it the schema is the same
        var unregisteredSchema = TestApi.Schema(unregistered, "Malformed").ToJsonString();

        Assert.Equal("{\"type\":\"string\"}", unregisteredSchema);
        Assert.Equal(unregisteredSchema, TestApi.Schema(registered, "Malformed").ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_ForeignEntryOnUnsupported_GetsEmptyName()
    {
        // Given: a schema transformer that writes [1] into the enum of Unsupported before the
        // registration, and the converter of Unsupported throws NotSupportedException
        await using var host = await TestApi.Start<UnsupportedResponse>(
            TestApi.RegisterAfterEntries<Unsupported>("[1]"));

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: the entry gets an empty name in both lists, and no description list exists
        TestApi.AssertEnum(TestApi.Schema(document, "Unsupported"), "[1]", "[\"\"]", null);
    }

    [JsonConverter(typeof(ThrowingConverter<Unsupported, NotSupportedException>))]
    internal enum Unsupported
    {
        One = 1,
    }

    [JsonConverter(typeof(ThrowingConverter<Malformed, JsonException>))]
    internal enum Malformed
    {
        One = 1,
    }

    internal sealed record UnsupportedResponse(Unsupported Value);

    internal sealed record MalformedResponse(Malformed Value);

    private sealed class ThrowingConverter<TEnum, TException> : JsonConverter<TEnum>
        where TEnum : struct, Enum
        where TException : Exception, new()
    {
        public override TEnum Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new TException();
        }

        public override void Write(
            Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            throw new TException();
        }
    }
}
