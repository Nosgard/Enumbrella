using System.Text.Json;
using System.Text.Json.Serialization;

namespace Enumbrella.Tests;

public class ConverterFailureTests
{
    [Fact]
    public async Task schema_stays_untouched_when_the_converter_cannot_write_values()
    {
        var document = await TestApi.Document<UnwritableResponse>(
            options => options.AddEnumDescriptions());

        var schema = document["components"]!["schemas"]!["FailsOnWrite"]!.AsObject();

        Assert.DoesNotContain(schema, property => property.Key == "enum"
            || property.Key.StartsWith("x-enum", StringComparison.Ordinal));
    }

    [Fact]
    public async Task unreadable_values_yield_empty_names()
    {
        var document = await TestApi.Document<UnreadableResponse>(
            options => options.AddEnumDescriptions());

        var schemas = document["components"]!["schemas"]!;

        Assert.Equal("[\"\"]", schemas["RejectedOnRead"]!["x-enum-varnames"]!.ToJsonString());
        Assert.Equal("[\"\"]", schemas["UnsupportedOnRead"]!["x-enum-varnames"]!.ToJsonString());
    }

    [JsonConverter(typeof(ThrowOnWriteConverter))]
    internal enum FailsOnWrite
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

    internal sealed record UnwritableResponse(FailsOnWrite Value);

    internal sealed record UnreadableResponse(
        RejectedOnRead Rejected, UnsupportedOnRead Unsupported);

    private sealed class ThrowOnWriteConverter : JsonConverter<FailsOnWrite>
    {
        public override FailsOnWrite Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return (FailsOnWrite)reader.GetInt32();
        }

        public override void Write(
            Utf8JsonWriter writer, FailsOnWrite value, JsonSerializerOptions options)
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
