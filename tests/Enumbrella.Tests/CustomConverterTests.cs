using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Enumbrella.Sample;
using Microsoft.AspNetCore.OpenApi;

namespace Enumbrella.Tests;

public class CustomConverterTests
{
    private static readonly Action<OpenApiOptions> Register =
        options => options.AddEnumDescriptions();

    [Fact]
    public async Task custom_converters_on_the_type_and_in_the_options_keep_the_shared_schema()
    {
        // Given: enums with a converter on the type and in the options, the latter also nullable

        // When: the document is generated with and without the registration
        var registered = await TestApi.Document<ConvertedResponse>(Register, AddOptionsConverter);
        var unregistered = await TestApi.Document<ConvertedResponse>(_ => { }, AddOptionsConverter);

        // Then: both schemas match and neither converter ran
        Assert.Equal(Schema(unregistered, "ByType"), Schema(registered, "ByType"));
        Assert.Equal(Schema(unregistered, "ByOptions"), Schema(registered, "ByOptions"));
        Assert.Equal(0, CountingConverter<ByType>.Calls);
        Assert.Equal(0, CountingConverter<ByOptions>.Calls);
    }

    [Fact]
    public async Task built_in_converters_keep_their_lists_next_to_custom_converters()
    {
        // Given: an integer enum and a string enum next to enums with custom converters

        // When: the document is generated
        var document = await TestApi.Document<ConvertedResponse>(Register, AddOptionsConverter);

        // Then: both list their values and both name lists
        var shade = document["components"]!["schemas"]!["Shade"]!;
        var names = "[\"Light\",\"Dark\"]";

        TestApi.AssertStatusComponent(document);
        Assert.Equal(names, shade["enum"]!.ToJsonString());
        Assert.Equal(names, shade["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, shade["x-enumNames"]!.ToJsonString());
    }

    [Fact]
    public async Task custom_converters_on_properties_keep_the_shared_schema()
    {
        // Given: an OrderStatus and an OrderStatus? property, both with a custom converter

        // When: the document is generated with and without the registration
        var registered = await TestApi.Document<PropertyConvertedResponse>(Register);
        var unregistered = await TestApi.Document<PropertyConvertedResponse>(_ => { });

        // Then: both documents match and the converter never ran
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<OrderStatus>.Calls);
    }

    [Fact]
    public async Task custom_converters_on_properties_keep_the_inline_schema()
    {
        // Given: the same properties in a host that keeps enums inline

        // When: the document is generated with and without the registration
        var registered = await TestApi.InlineEnumDocument<PropertyConvertedResponse>(Register);
        var unregistered = await TestApi.InlineEnumDocument<PropertyConvertedResponse>(_ => { });

        // Then: both documents match and the converter never ran
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<OrderStatus>.Calls);
    }

    [Fact]
    public async Task nullable_enum_with_a_custom_converter_keeps_the_shared_schema()
    {
        // Given: a nullable enum whose type carries a custom converter

        // When: the document is generated with and without the registration
        var registered = await TestApi.Document<NullableConvertedResponse>(Register);
        var unregistered = await TestApi.Document<NullableConvertedResponse>(_ => { });

        // Then: both documents match and the converter never ran
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<ByType>.Calls);
    }

    [Fact]
    public async Task nullable_enum_with_a_custom_converter_keeps_the_inline_schema()
    {
        // Given: the same nullable enum in a host that keeps enums inline

        // When: the document is generated with and without the registration
        var registered = await TestApi.InlineEnumDocument<NullableConvertedResponse>(Register);
        var unregistered = await TestApi.InlineEnumDocument<NullableConvertedResponse>(_ => { });

        // Then: both documents match and the converter never ran
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<ByType>.Calls);
    }

    [Fact]
    public async Task custom_converters_on_the_type_and_in_the_options_keep_the_inline_schema()
    {
        // Given: plain enums with converters on the type and in the options, and a nullable one
        // in a host that keeps enums inline

        // When: the document is generated with and without the registration
        var registered = await TestApi.InlineEnumDocument<TypeAndOptionsResponse>(
            Register, AddOptionsConverter);
        var unregistered = await TestApi.InlineEnumDocument<TypeAndOptionsResponse>(
            _ => { }, AddOptionsConverter);

        // Then: both documents match and neither converter ran
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<ByType>.Calls);
        Assert.Equal(0, CountingConverter<ByOptions>.Calls);
    }

    private static void AddOptionsConverter(JsonSerializerOptions options)
    {
        options.Converters.Add(new CountingConverter<ByOptions>());
    }

    private static string Schema(JsonNode document, string name)
    {
        return document["components"]!["schemas"]![name]!.ToJsonString();
    }

    [JsonConverter(typeof(CountingConverter<ByType>))]
    internal enum ByType
    {
        One = 1,
    }

    internal enum ByOptions
    {
        One = 1,
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    internal enum Shade
    {
        Light = 1,
        Dark = 2,
    }

    internal sealed record ConvertedResponse(
        ByType ByType, ByOptions ByOptions, ByOptions? Optional, OrderStatus Status, Shade Shade);

    internal sealed record PropertyConvertedResponse(
        [property: JsonConverter(typeof(CountingConverter<OrderStatus>))] OrderStatus Plain,
        [property: JsonConverter(typeof(CountingConverter<OrderStatus>))] OrderStatus? Optional);

    internal sealed record NullableConvertedResponse(ByType? Value);

    internal sealed record TypeAndOptionsResponse(
        ByType ByType, ByOptions ByOptions, ByOptions? Optional);

    // Counts every call and throws, so a test sees both a call and a failing document.
    private sealed class CountingConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        public static int Calls { get; private set; }

        public override TEnum Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Calls++;
            throw new InvalidOperationException();
        }

        public override void Write(
            Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            Calls++;
            throw new InvalidOperationException();
        }
    }
}
