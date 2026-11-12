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
    public async Task AddEnumDescriptions_CustomConvertersOnTypeAndInOptions_KeepSharedSchema()
    {
        // Given: enums with a converter on the type and in the options, the latter also nullable
        await using var registeredHost = await TestApi.Start<ConvertedResponse>(
            Register, AddOptionsConverter);
        await using var unregisteredHost = await TestApi.Start<ConvertedResponse>(
            _ => { }, AddOptionsConverter);

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: both schemas match and neither converter ran
        Assert.Equal(Schema(unregistered, "ByType"), Schema(registered, "ByType"));
        Assert.Equal(Schema(unregistered, "ByOptions"), Schema(registered, "ByOptions"));
        Assert.Equal(0, CountingConverter<ByType>.Calls);
        Assert.Equal(0, CountingConverter<ByOptions>.Calls);
    }

    [Fact]
    public async Task AddEnumDescriptions_BuiltInNextToCustomConverters_ListValuesAndBothNameLists()
    {
        // Given: an integer enum and a string enum next to enums with custom converters
        await using var host = await TestApi.Start<ConvertedResponse>(
            Register, AddOptionsConverter);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: both list their values and both name lists
        var shade = document["components"]!["schemas"]!["Shade"]!;
        var names = "[\"Light\",\"Dark\"]";

        TestApi.AssertStatusComponent(document);
        Assert.Equal(names, shade["enum"]!.ToJsonString());
        Assert.Equal(names, shade["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, shade["x-enumNames"]!.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_CustomConvertersOnProperties_KeepSharedSchema()
    {
        // Given: an OrderStatus and an OrderStatus? property, both with a custom converter
        await using var registeredHost = await TestApi.Start<PropertyConvertedResponse>(Register);
        await using var unregisteredHost = await TestApi.Start<PropertyConvertedResponse>(_ => { });

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: both documents match and the converter never ran
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<OrderStatus>.Calls);
    }

    [Fact]
    public async Task AddEnumDescriptions_CustomConvertersOnProperties_KeepInlineSchema()
    {
        // Given: the same properties in a host that keeps enums inline
        await using var registeredHost =
            await TestApi.StartInlineEnums<PropertyConvertedResponse>(Register);
        await using var unregisteredHost =
            await TestApi.StartInlineEnums<PropertyConvertedResponse>(_ => { });

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: both documents match and the converter never ran
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<OrderStatus>.Calls);
    }

    [Fact]
    public async Task AddEnumDescriptions_NullableEnumWithCustomConverter_KeepsSharedSchema()
    {
        // Given: a nullable enum whose type carries a custom converter
        await using var registeredHost = await TestApi.Start<NullableConvertedResponse>(Register);
        await using var unregisteredHost = await TestApi.Start<NullableConvertedResponse>(_ => { });

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: both documents match and the converter never ran
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<ByType>.Calls);
    }

    [Fact]
    public async Task AddEnumDescriptions_NullableEnumWithCustomConverter_KeepsInlineSchema()
    {
        // Given: the same nullable enum in a host that keeps enums inline
        await using var registeredHost =
            await TestApi.StartInlineEnums<NullableConvertedResponse>(Register);
        await using var unregisteredHost =
            await TestApi.StartInlineEnums<NullableConvertedResponse>(_ => { });

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: both documents match and the converter never ran
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<ByType>.Calls);
    }

    [Fact]
    public async Task AddEnumDescriptions_CustomConvertersOnTypeAndInOptions_KeepInlineSchema()
    {
        // Given: plain enums with converters on the type and in the options, and a nullable one
        // in a host that keeps enums inline
        await using var registeredHost = await TestApi.StartInlineEnums<TypeAndOptionsResponse>(
            Register, AddOptionsConverter);
        await using var unregisteredHost = await TestApi.StartInlineEnums<TypeAndOptionsResponse>(
            _ => { }, AddOptionsConverter);

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

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
