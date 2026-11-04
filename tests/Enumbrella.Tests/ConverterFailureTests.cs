using System.Text.Json;
using System.Text.Json.Serialization;

namespace Enumbrella.Tests;

public class ConverterFailureTests
{
    [Fact]
    public async Task schema_stays_untouched_when_the_converter_cannot_write_values()
    {
        var document = await TestApi.Document<OpaqueResponse>(
            options => options.AddEnumDescriptions());

        var schemas = document["components"]!["schemas"]!.AsObject();

        Assert.All(schemas, schema => Assert.DoesNotContain(
            schema.Value!.AsObject(),
            property => property.Key == "enum"
                || property.Key.StartsWith("x-enum", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task document_is_served_when_the_converter_cannot_read_values()
    {
        var document = await TestApi.Document<UnreadableResponse>(
            options => options.AddEnumDescriptions());

        var schemas = document["components"]!["schemas"]!;

        Assert.NotNull(schemas["RejectedOnRead"]);
        Assert.NotNull(schemas["UnsupportedOnRead"]);
    }

    [JsonConverter(typeof(ThrowingConverter))]
    internal enum Opaque
    {
        One = 1,
    }

    [JsonConverter(typeof(RejectingReadConverter))]
    internal enum RejectedOnRead
    {
        One = 1,
    }

    [JsonConverter(typeof(UnsupportedReadConverter))]
    internal enum UnsupportedOnRead
    {
        One = 1,
    }

    internal sealed record OpaqueResponse(Opaque Opaque);

    internal sealed record UnreadableResponse(
        RejectedOnRead Rejected, UnsupportedOnRead Unsupported);

    private sealed class ThrowingConverter : JsonConverter<Opaque>
    {
        public override Opaque Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(
            Utf8JsonWriter writer, Opaque value, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RejectingReadConverter : JsonConverter<RejectedOnRead>
    {
        public override RejectedOnRead Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new JsonException();
        }

        public override void Write(
            Utf8JsonWriter writer, RejectedOnRead value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((int)value);
        }
    }

    private sealed class UnsupportedReadConverter : JsonConverter<UnsupportedOnRead>
    {
        public override UnsupportedOnRead Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(
            Utf8JsonWriter writer, UnsupportedOnRead value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((int)value);
        }
    }
}
