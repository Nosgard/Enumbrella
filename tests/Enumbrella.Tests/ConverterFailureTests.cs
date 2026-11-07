using System.Text.Json;
using System.Text.Json.Serialization;

namespace Enumbrella.Tests;

public class ConverterFailureTests
{
    [Fact]
    public async Task schema_stays_untouched_when_the_converter_cannot_write_values()
    {
        string[] names = ["FailsOnWrite", "RejectedOnWrite"];
        var document = await TestApi.Document<UnwritableResponse>(
            options => options.AddEnumDescriptions());

        var schemas = document["components"]!["schemas"]!;

        Assert.All(names, name => Assert.DoesNotContain(
            schemas[name]!.AsObject(),
            property => property.Key == "enum"
                || property.Key.StartsWith("x-enum", StringComparison.Ordinal)));
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

    [Fact]
    public async Task value_read_back_as_an_undeclared_member_yields_an_empty_name()
    {
        var document = await TestApi.Document<UndeclaredResponse>(
            options => options.AddEnumDescriptions());

        var schema = document["components"]!["schemas"]!["UndeclaredOnRead"]!;

        Assert.Equal("[\"\"]", schema["x-enum-varnames"]!.ToJsonString());
    }

    [JsonConverter(typeof(ThrowOnWriteConverter))]
    internal enum FailsOnWrite
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

    internal sealed record UnwritableResponse(FailsOnWrite Value, RejectedOnWrite Rejected);

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
