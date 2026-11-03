using System.Reflection;
using Enumbrella.Sample;
using Microsoft.AspNetCore.OpenApi;

namespace Enumbrella.Tests;

public class RegistrationTests
{
    [Fact]
    public async Task schema_of_OrderStatus_has_no_enum_without_registration()
    {
        var document = await TestApi.Document<Order>(_ => { });

        var schema = document["components"]!["schemas"]!["OrderStatus"]!;

        Assert.Null(schema["enum"]);
    }

    [Fact]
    public void AddEnumDescriptions_throws_on_null_options()
    {
        Assert.Throws<ArgumentNullException>(() => EnumbrellaOpenApiOptionsExtensions.AddEnumDescriptions(null!));
    }

    [Fact]
    public void AddEnumDescriptions_returns_the_same_options_instance()
    {
        var options = new OpenApiOptions();

        var returned = options.AddEnumDescriptions();

        Assert.Same(options, returned);
    }

    [Fact]
    public void assembly_exposes_exactly_one_public_type_with_one_public_method()
    {
        var assembly = typeof(EnumbrellaOpenApiOptionsExtensions).Assembly;

        var type = Assert.Single(assembly.GetExportedTypes());
        var method = Assert.Single(type.GetMethods(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly));

        Assert.Equal("Enumbrella.EnumbrellaOpenApiOptionsExtensions", type.FullName);
        Assert.Equal("AddEnumDescriptions", method.Name);
    }
}
