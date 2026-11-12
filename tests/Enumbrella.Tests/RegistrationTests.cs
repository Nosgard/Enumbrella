using System.Reflection;
using Enumbrella.Sample;
using Microsoft.AspNetCore.OpenApi;

namespace Enumbrella.Tests;

public class RegistrationTests
{
    [Fact]
    public async Task AddEnumDescriptions_SchemaOfOrderStatusWithoutRegistration_HasNoEnum()
    {
        // Given: a host without the registration
        await using var host = await TestApi.Start<Order>(_ => { });

        // When: the document is generated
        var document = await TestApi.Fetch(host);

        var schema = document["components"]!["schemas"]!["OrderStatus"]!;

        // Then: OrderStatus has no value list
        Assert.Null(schema["enum"]);
    }

    [Fact]
    public async Task AddEnumDescriptions_RegisteredAndUnregisteredHost_OnlyRegisteredWritesXEnum()
    {
        // Given: one host with the registration and one without
        await using var sampleApi = SampleApi.Start();
        await using var unregisteredHost = await TestApi.Start<Order>(_ => { });

        // When: both documents are generated
        var registered = await SampleApi.Fetch(sampleApi);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        var registeredStatus = registered["components"]!["schemas"]!["OrderStatus"]!;

        // Then: only the registered document carries x-enum extensions
        Assert.NotNull(registeredStatus["x-enum-varnames"]);
        Assert.DoesNotContain("\"x-enum", unregistered.ToJsonString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AddEnumDescriptions_NullOptions_ThrowsArgumentNullException()
    {
        // Given: no options instance, passed as null in the call below

        // When: AddEnumDescriptions runs

        // Then: it throws ArgumentNullException
        Assert.Throws<ArgumentNullException>(
            () => EnumbrellaOpenApiOptionsExtensions.AddEnumDescriptions(null!));
    }

    [Fact]
    public void AddEnumDescriptions_NewOptions_ReturnsSameOptionsInstance()
    {
        // Given: a new options instance
        var options = new OpenApiOptions();

        // When: AddEnumDescriptions runs
        var returned = options.AddEnumDescriptions();

        // Then: the caller can keep chaining on the same instance
        Assert.Same(options, returned);
    }

    [Fact]
    public void EnumbrellaOpenApiOptionsExtensions_Assembly_IsOnlyPublicTypeWithOnePublicMethod()
    {
        // Given: the package assembly
        var assembly = typeof(EnumbrellaOpenApiOptionsExtensions).Assembly;
        var declared = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance
            | BindingFlags.DeclaredOnly;

        // When: its exported types and their public methods are read
        var type = Assert.Single(assembly.GetExportedTypes());
        var method = Assert.Single(type.GetMethods(declared));

        // Then: AddEnumDescriptions is the only public entry point
        Assert.Equal("Enumbrella.EnumbrellaOpenApiOptionsExtensions", type.FullName);
        Assert.Equal("AddEnumDescriptions", method.Name);
    }

    [Fact]
    public void EnumbrellaOpenApiOptionsExtensions_Assembly_HoldsNoGeneratedOpenApiTypes()
    {
        // Given: the package assembly
        var assembly = typeof(EnumbrellaOpenApiOptionsExtensions).Assembly;

        // When: its types are listed

        // Then: none comes from a generated OpenAPI namespace
        Assert.DoesNotContain(assembly.GetTypes(), type => type.Namespace is { } ns
            && ns.StartsWith("Microsoft.AspNetCore.OpenApi", StringComparison.Ordinal));
    }
}
