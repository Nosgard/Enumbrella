using System.Text.Json;
using System.Text.Json.Serialization;
using Enumbrella.Sample;

namespace Enumbrella.Tests;

public class CustomConverterTests
{
    [Fact]
    public async Task AddEnumDescriptions_CustomConvertersOnTypeAndInOptions_KeepSharedSchema()
    {
        // Given: enums with a converter on the type and in the options, the latter also nullable
        CountingConverter<ByType>.Reset();
        CountingConverter<ByOptions>.Reset();

        await using var registeredHost = await TestApi.Start<TypeAndOptionsResponse>(
            TestApi.Register, AddOptionsConverter);
        await using var unregisteredHost = await TestApi.Start<TypeAndOptionsResponse>(
            _ => { }, AddOptionsConverter);

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: ByType and ByOptions are components, both documents match and neither converter ran
        var schemas = TestApi.Schemas(registered);

        Assert.Contains("ByType", schemas);
        Assert.Contains("ByOptions", schemas);
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<ByType>.Calls);
        Assert.Equal(0, CountingConverter<ByOptions>.Calls);
    }

    [Fact]
    public async Task AddEnumDescriptions_BuiltInNextToCustomConverters_DescribeOnlyDescribedEnums()
    {
        // Given: an integer enum and a string enum next to enums with custom converters
        CountingConverter<ByType>.Reset();
        CountingConverter<ByOptions>.Reset();

        await using var host = await TestApi.Start<ConvertedResponse>(
            TestApi.Register, AddOptionsConverter);

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        // Then: both list their values and both name lists, Shade has no descriptions, ByType and
        // ByOptions stay untouched, and neither custom converter ran
        var names = "[\"Light\",\"Dark\"]";

        TestApi.AssertStatusComponent(document);
        TestApi.AssertEnum(TestApi.Schema(document, "Shade"), names, names, null);
        TestApi.AssertUntouched(document, "ByType", "ByOptions");
        Assert.Equal(0, CountingConverter<ByType>.Calls);
        Assert.Equal(0, CountingConverter<ByOptions>.Calls);
    }

    [Fact]
    public async Task AddEnumDescriptions_CustomConvertersOnProperties_KeepSharedSchema()
    {
        // Given: an OrderStatus and an OrderStatus? property, both with a custom converter
        CountingConverter<OrderStatus>.Reset();

        await using var registeredHost =
            await TestApi.Start<PropertyConvertedResponse>(TestApi.Register);
        await using var unregisteredHost = await TestApi.Start<PropertyConvertedResponse>(_ => { });

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: OrderStatus is a component, both documents match and the converter never ran
        Assert.Contains("OrderStatus", TestApi.Schemas(registered));
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<OrderStatus>.Calls);
    }

    [Fact]
    public async Task AddEnumDescriptions_CustomConvertersOnProperties_KeepInlineSchema()
    {
        // Given: the same properties in a host that keeps enums inline
        CountingConverter<OrderStatus>.Reset();

        await using var registeredHost =
            await TestApi.StartInlineEnums<PropertyConvertedResponse>(TestApi.Register);
        await using var unregisteredHost =
            await TestApi.StartInlineEnums<PropertyConvertedResponse>(_ => { });

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: OrderStatus is no component, both documents match and the converter never ran
        Assert.DoesNotContain("OrderStatus", TestApi.Schemas(registered));
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<OrderStatus>.Calls);
    }

    [Fact]
    public async Task AddEnumDescriptions_NullableEnumWithCustomConverter_KeepsSharedSchema()
    {
        // Given: a nullable enum whose type carries a custom converter
        CountingConverter<ByType>.Reset();

        await using var registeredHost =
            await TestApi.Start<NullableConvertedResponse>(TestApi.Register);
        await using var unregisteredHost = await TestApi.Start<NullableConvertedResponse>(_ => { });

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: ByType is a component, both documents match and the converter never ran
        Assert.Contains("ByType", TestApi.Schemas(registered));
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<ByType>.Calls);
    }

    [Fact]
    public async Task AddEnumDescriptions_NullableEnumWithCustomConverter_KeepsInlineSchema()
    {
        // Given: the same nullable enum in a host that keeps enums inline
        CountingConverter<ByType>.Reset();

        await using var registeredHost =
            await TestApi.StartInlineEnums<NullableConvertedResponse>(TestApi.Register);
        await using var unregisteredHost =
            await TestApi.StartInlineEnums<NullableConvertedResponse>(_ => { });

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: ByType is no component, both documents match and the converter never ran
        Assert.DoesNotContain("ByType", TestApi.Schemas(registered));
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<ByType>.Calls);
    }

    [Fact]
    public async Task AddEnumDescriptions_CustomConvertersOnTypeAndInOptions_KeepInlineSchema()
    {
        // Given: plain enums with converters on the type and in the options, and a nullable one
        // in a host that keeps enums inline
        CountingConverter<ByType>.Reset();
        CountingConverter<ByOptions>.Reset();

        await using var registeredHost = await TestApi.StartInlineEnums<TypeAndOptionsResponse>(
            TestApi.Register, AddOptionsConverter);
        await using var unregisteredHost = await TestApi.StartInlineEnums<TypeAndOptionsResponse>(
            _ => { }, AddOptionsConverter);

        // When: the document is generated with and without the registration
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: ByType and ByOptions are no components, both documents match and neither
        // converter ran
        var schemas = TestApi.Schemas(registered);

        Assert.DoesNotContain("ByType", schemas);
        Assert.DoesNotContain("ByOptions", schemas);
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
        Assert.Equal(0, CountingConverter<ByType>.Calls);
        Assert.Equal(0, CountingConverter<ByOptions>.Calls);
    }

    private static void AddOptionsConverter(JsonSerializerOptions options)
    {
        options.Converters.Add(new CountingConverter<ByOptions>());
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

        public static void Reset()
        {
            Calls = 0;
        }

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
