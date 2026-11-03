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

    [JsonConverter(typeof(ThrowingConverter))]
    internal enum Opaque
    {
        One = 1,
    }

    internal sealed record OpaqueResponse(Opaque Opaque);

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
}
