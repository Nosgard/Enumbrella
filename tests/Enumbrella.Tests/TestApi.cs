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
    // Options

    public static readonly Action<OpenApiOptions> Register =
        options => options.AddEnumDescriptions();

    public static readonly Action<OpenApiOptions> RegisterTwice =
        options => options.AddEnumDescriptions().AddEnumDescriptions();

    // Given

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

    public static Task<WebApplication> StartEndpoints(params Action<WebApplication>[] endpoints)
    {
        return Start(
            configureJson: null,
            host => Array.ForEach(endpoints, map => map(host)),
            ("v1", Register));
    }

    public static void MapResponse<TResponse>(WebApplication app)
    {
        app.MapGet("/", () => default(TResponse)!);
    }

    public static void MapNullableStatus(WebApplication app)
    {
        app.MapGet("/nullable", () => default(OrderStatus?));
    }

    public static void MapPlainStatus(WebApplication app)
    {
        app.MapGet("/plain", () => default(OrderStatus));
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

    private static string? InlineEnumReferenceId(JsonTypeInfo typeInfo)
    {
        var type = Nullable.GetUnderlyingType(typeInfo.Type) ?? typeInfo.Type;

        return type.IsEnum ? null : OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
    }

    // When

    public static async Task<JsonNode> Fetch(WebApplication app, string document = "v1")
    {
        using var client = app.GetTestClient();

        return await Fetch(client, document);
    }

    // Every document passes through here, so a list that does not match its enum fails the
    // fetch of any test, whichever schema it sits on.
    public static async Task<JsonNode> Fetch(HttpClient client, string document = "v1")
    {
        var json = await client.GetStringAsync($"/openapi/{document}.json");
        var parsed = JsonNode.Parse(json)!;

        foreach (var schema in Objects(parsed).Where(HasEnumList))
        {
            AssertListsMatchEnum(schema);
        }

        return parsed;
    }

    private static bool HasEnumList(JsonObject schema)
    {
        return schema.ContainsKey("x-enum-varnames")
            || schema.ContainsKey("x-enumNames")
            || schema.ContainsKey("x-enum-descriptions");
    }

    private static void AssertListsMatchEnum(JsonObject schema)
    {
        var path = schema.GetPath();
        var count = schema["enum"]?.AsArray().Count;
        var names = schema["x-enum-varnames"]?.AsArray();
        var descriptions = schema["x-enum-descriptions"]?.AsArray();

        Assert.True(count is not null, $"{path} has an enum list extension but no enum");
        Assert.True(names is not null, $"{path} has an enum list extension but no x-enum-varnames");
        Assert.True(names.Count == count, $"{path}: x-enum-varnames is not as long as enum");
        Assert.True(
            schema["x-enumNames"]?.ToJsonString() == names.ToJsonString(),
            $"{path}: x-enumNames differs from x-enum-varnames");
        Assert.True(
            descriptions is null || descriptions.Count == count,
            $"{path}: x-enum-descriptions is not as long as enum");
    }

    // Then

    public static JsonObject Schemas(JsonNode document)
    {
        return document["components"]!["schemas"]!.AsObject();
    }

    public static JsonNode Schema(JsonNode document, string name)
    {
        return Schemas(document)[name]!;
    }

    public static JsonNode Property(JsonNode document, string component, string property)
    {
        return Schema(document, component)["properties"]![property]!;
    }

    public static IEnumerable<JsonObject> Objects(JsonNode? node)
    {
        return node switch
        {
            JsonObject obj => [obj, .. obj.SelectMany(keyword => Objects(keyword.Value))],
            JsonArray array => array.SelectMany(Objects),
            _ => [],
        };
    }

    // Checks enum and all three lists of one schema; null descriptions means no description list.
    public static void AssertEnum(
        JsonNode schema, string values, string names, string? descriptions)
    {
        Assert.Equal(values, schema["enum"]!.ToJsonString());
        Assert.Equal(names, schema["x-enum-varnames"]!.ToJsonString());
        Assert.Equal(names, schema["x-enumNames"]!.ToJsonString());
        Assert.Equal(descriptions, schema["x-enum-descriptions"]?.ToJsonString());
    }

    public static void AssertStatusComponent(JsonNode document)
    {
        AssertEnum(
            Schema(document, "OrderStatus"),
            "[0,1,2]",
            "[\"Pending\",\"Approved\",\"Rejected\"]",
            "[\"Waiting for approval\",\"Approved\",\"\"]");
    }
}
