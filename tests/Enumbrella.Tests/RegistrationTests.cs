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

        // When: the document is generated
        var document = await TestApi.Document<Order>(_ => { });

        var schema = document["components"]!["schemas"]!["OrderStatus"]!;

        // Then: OrderStatus has no value list
        Assert.Null(schema["enum"]);
    }

    [Fact]
    public async Task AddEnumDescriptions_RegisteredAndUnregisteredHost_OnlyRegisteredWritesXEnum()
    {
        // Given: one host with the registration and one without

        // When: both documents are generated
        var registered = await SampleApi.Document();
        var unregistered = await TestApi.Document<Order>(_ => { });

        var registeredStatus = registered["components"]!["schemas"]!["OrderStatus"]!;

        // Then: only the registered document carries x-enum extensions
        Assert.NotNull(registeredStatus["x-enum-varnames"]);
        Assert.DoesNotContain("\"x-enum", unregistered.ToJsonString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AddEnumDescriptions_NullOptions_ThrowsArgumentNullException()
    {
        // Given: no options instance

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
