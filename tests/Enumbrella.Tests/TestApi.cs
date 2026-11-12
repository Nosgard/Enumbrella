using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Enumbrella.Sample;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Enumbrella.Tests;

internal static class TestApi
{
    // Document, EndpointDocument and InlineEnumDocument serve tests whose starting state is the
    // finished document, such as the list length test. Other tests start a host and fetch.
    public static async Task<JsonNode> Document<TResponse>(
        Action<OpenApiOptions> configure, Action<JsonSerializerOptions>? configureJson = null)
    {
        await using var app = await Start<TResponse>(configure, configureJson);

        return await Fetch(app);
    }

    public static Task<WebApplication> Start<TResponse>(
        Action<OpenApiOptions> configure, Action<JsonSerializerOptions>? configureJson = null)
    {
        return Start(configureJson, MapResponse<TResponse>, ("v1", configure));
    }

    public static async Task<WebApplication> Start(
        Action<JsonSerializerOptions>? configureJson,
        Action<WebApplication> mapEndpoints,
        params (string Name, Action<OpenApiOptions> Configure)[] documents)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.ConfigureHttpJsonOptions(
            json => configureJson?.Invoke(json.SerializerOptions));

        foreach (var (name, configure) in documents)
        {
            builder.Services.AddOpenApi(name, configure);
        }

        var app = builder.Build();
        app.MapOpenApi();
        mapEndpoints(app);
        await app.StartAsync();

        return app;
    }

    public static async Task<JsonNode> Fetch(WebApplication app, string document = "v1")
    {
        using var client = app.GetTestClient();
        var json = await client.GetStringAsync($"/openapi/{document}.json");

        return JsonNode.Parse(json)!;
    }

    public static void MapResponse<TResponse>(WebApplication app)
    {
        app.MapGet("/", () => default(TResponse)!);
    }

    public static async Task<JsonNode> EndpointDocument(params Action<WebApplication>[] endpoints)
    {
        await using var app = await StartEndpoints(endpoints);

        return await Fetch(app);
    }

    public static Task<WebApplication> StartEndpoints(params Action<WebApplication>[] endpoints)
    {
        return Start(
            configureJson: null,
            host => Array.ForEach(endpoints, map => map(host)),
            ("v1", options => options.AddEnumDescriptions()));
    }

    public static void MapNullableStatus(WebApplication app)
    {
        app.MapGet("/nullable", () => default(OrderStatus?));
    }

    public static void MapPlainStatus(WebApplication app)
    {
        app.MapGet("/plain", () => default(OrderStatus));
    }

    public static async Task<JsonNode> InlineEnumDocument<TResponse>(
        Action<OpenApiOptions> configure, Action<JsonSerializerOptions>? configureJson = null)
    {
        await using var app = await StartInlineEnums<TResponse>(configure, configureJson);

        return await Fetch(app);
    }

    public static Task<WebApplication> StartInlineEnums<TResponse>(
        Action<OpenApiOptions> configure, Action<JsonSerializerOptions>? configureJson = null)
    {
        return Start<TResponse>(
            options =>
            {
                options.CreateSchemaReferenceId = InlineEnumReferenceId;
                configure(options);
            },
            configureJson);
    }

    public static Task WriteUnreadableStatusEntry(
        OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken token)
    {
        if (context.JsonTypeInfo.Type == typeof(OrderStatus))
        {
            schema.Enum = [JsonValue.Create("unknown"), JsonValue.Create(0)];
        }

        return Task.CompletedTask;
    }

    public static void AssertStatusComponent(JsonNode document)
    {
        var schema = document["components"]!["schemas"]!["OrderStatus"]!;
        var names = "[\"Pending\",\"Approved\",\"Rejected\"]";

        Assert.Equal("[0,1,2]", schema["enum"]!.ToJsonString());
        Assert.Equal(names, schema["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, schema["x-enumNames"]!.ToJsonString());
    }

    private static string? InlineEnumReferenceId(JsonTypeInfo typeInfo)
    {
        var type = Nullable.GetUnderlyingType(typeInfo.Type) ?? typeInfo.Type;

        return type.IsEnum ? null : OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
    }
}
