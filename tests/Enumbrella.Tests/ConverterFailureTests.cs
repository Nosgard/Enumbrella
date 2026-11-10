using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Enumbrella.Tests;

public class ConverterFailureTests
{
    [Fact]
    public async Task schema_stays_untouched_when_the_converter_cannot_write_values()
    {
        // Given: two enums whose converters throw on every write

        // When: the document is generated
        var document = await TestApi.Document<UnwritableResponse>(
            options => options.AddEnumDescriptions());

        // Then: neither schema gets a value list or a name list
        AssertUntouched(document, "UnsupportedOnWrite", "RejectedOnWrite");
    }

    [Fact]
    public async Task schema_stays_untouched_when_the_converter_cannot_read_values()
    {
        // Given: two enums whose converters throw on every read

        // When: the document is generated
        var document = await TestApi.Document<UnreadableResponse>(
            options => options.AddEnumDescriptions());

        // Then: neither schema gets a value list or a name list
        AssertUntouched(document, "RejectedOnRead", "UnsupportedOnRead");
    }

    [Fact]
    public async Task schema_stays_untouched_when_the_converter_reads_an_undeclared_member()
    {
        // Given: a converter that reads each value as an undeclared member

        // When: the document is generated
        var document = await TestApi.Document<UndeclaredResponse>(
            options => options.AddEnumDescriptions());

        // Then: the schema gets no value list and no name list
        AssertUntouched(document, "UndeclaredOnRead");
    }

    private static void AssertUntouched(JsonNode document, params string[] names)
    {
        var schemas = document["components"]!["schemas"]!;

        Assert.All(names, name => Assert.DoesNotContain(
            schemas[name]!.AsObject(),
            property => property.Key == "enum"
                || property.Key.StartsWith("x-enum", StringComparison.Ordinal)));
    }

    [JsonConverter(typeof(UnsupportedWriteConverter))]
    internal enum UnsupportedOnWrite
    {
        One = 1,
    }

    [JsonConverter(typeof(RejectingWriteConverter))]
    internal enum RejectedOnWrite
    {
        One = 1,
    }

    [JsonConverter(typeof(UndeclaredReadConverter))]
    internal enum UndeclaredOnRead
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

    internal sealed record UnwritableResponse(
        UnsupportedOnWrite Unsupported, RejectedOnWrite Rejected);

    internal sealed record UnreadableResponse(
        RejectedOnRead Rejected, UnsupportedOnRead Unsupported);

    internal sealed record UndeclaredResponse(UndeclaredOnRead Value);

    private sealed class RejectingWriteConverter : JsonConverter<RejectedOnWrite>
    {
        public override RejectedOnWrite Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return (RejectedOnWrite)reader.GetInt32();
        }

        public override void Write(
            Utf8JsonWriter writer, RejectedOnWrite value, JsonSerializerOptions options)
        {
            throw new JsonException();
        }
    }

    private sealed class UndeclaredReadConverter : JsonConverter<UndeclaredOnRead>
    {
        public override UndeclaredOnRead Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return (UndeclaredOnRead)99;
        }

        public override void Write(
            Utf8JsonWriter writer, UndeclaredOnRead value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((int)value);
        }
    }

    private sealed class UnsupportedWriteConverter : JsonConverter<UnsupportedOnWrite>
    {
        public override UnsupportedOnWrite Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return (UnsupportedOnWrite)reader.GetInt32();
        }

        public override void Write(
            Utf8JsonWriter writer, UnsupportedOnWrite value, JsonSerializerOptions options)
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
