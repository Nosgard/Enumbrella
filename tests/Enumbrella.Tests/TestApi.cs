using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Enumbrella.Sample;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Enumbrella.Tests;

internal static class TestApi
{
    // Options

    public static readonly Action<OpenApiOptions> Register =
        options => options.AddEnumDescriptions();

    public static readonly Action<OpenApiOptions> RegisterTwice =
        options => options.AddEnumDescriptions().AddEnumDescriptions();

    public static readonly Action<JsonSerializerOptions> StringEnums =
        json => json.Converters.Add(new JsonStringEnumConverter());

    public static Action<OpenApiOptions> RegisterAfterEntries<TEnum>(string entries)
    {
        return WithEntries<TEnum>(entries, Register);
    }

    // Adds a schema transformer that writes entries into the enum of TEnum, then applies configure.
    public static Action<OpenApiOptions> WithEntries<TEnum>(
        string entries, Action<OpenApiOptions> configure)
    {
        return options => configure(options.AddSchemaTransformer(WriteEntries<TEnum>(entries)));
    }

    // Adds a schema transformer that sets the string type on TEnum, then applies configure.
    public static Action<OpenApiOptions> WithStringType<TEnum>(Action<OpenApiOptions> configure)
    {
        return options => configure(options.AddSchemaTransformer(WriteStringType<TEnum>));
    }

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
        return Start<TResponse>(WithInlineEnums(configure), configureJson);
    }

    public static Task<WebApplication> StartLocations<TValue>(Action<OpenApiOptions> configure)
        where TValue : struct
    {
        return Start(configureJson: null, MapLocations<TValue>, ("v1", configure));
    }

    public static Task<WebApplication> StartLocations<TResponse, TValue>(
        Action<OpenApiOptions> configure, Action<JsonSerializerOptions>? configureJson = null)
        where TValue : struct
    {
        return Start(
            configureJson,
            app =>
            {
                MapResponse<TResponse>(app);
                MapLocations<TValue>(app);
            },
            ("v1", configure));
    }

    public static Task<WebApplication> StartInlineLocations<TResponse, TValue>(
        Action<OpenApiOptions> configure, Action<JsonSerializerOptions>? configureJson = null)
        where TValue : struct
    {
        return StartLocations<TResponse, TValue>(WithInlineEnums(configure), configureJson);
    }

    // Maps OrderStatus only as the nullable query parameter status, so its component comes from
    // there.
    public static Task<WebApplication> StartNullableQuery(Action<OpenApiOptions> configure)
    {
        return Start(
            configureJson: null,
            app => app.MapGet("/nullable-query", (OrderStatus? status) => Results.NoContent()),
            ("v1", configure));
    }

    public static Task<WebApplication> StartEndpoints(params Action<WebApplication>[] endpoints)
    {
        return Start(
            configureJson: null,
            host => Array.ForEach(endpoints, map => map(host)),
            ("v1", Register));
    }

    public static Task<WebApplication> StartBothDocuments<TResponse>(
        Action<JsonSerializerOptions>? configureJson = null)
    {
        return Start(configureJson, MapResponse<TResponse>, ("v1", Register), ("v2", _ => { }));
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

    // The handlers with TValue go through typed delegates, because RouteHandlerAnalyzer throws on
    // a type parameter in a lambda passed straight to MapGet or MapPost (AD0001).
    private static void MapLocations<TValue>(WebApplication app)
        where TValue : struct
    {
        Func<TValue, int, IResult> statusAndId = (status, id) => Results.NoContent();
        Func<TValue?, IResult> nullableStatus = status => Results.NoContent();
        Func<TValue[], IResult> statusArray = statuses => Results.NoContent();

        app.MapGet("/path/{status}/{id}", statusAndId);
        app.MapGet("/query", statusAndId);
        app.MapGet("/nullable-query", nullableStatus);
        app.MapPost("/body", statusArray);
        app.MapPost("/numbers", (int[] numbers) => Results.NoContent());
    }

    private static Func<OpenApiSchema, OpenApiSchemaTransformerContext, CancellationToken, Task>
        WriteEntries<TEnum>(string entries)
    {
        return (schema, context, _) =>
        {
            if (context.JsonTypeInfo.Type == typeof(TEnum))
            {
                schema.Enum = JsonSerializer.Deserialize<List<JsonNode>>(entries)!;
            }

            return Task.CompletedTask;
        };
    }

    private static Task WriteStringType<TEnum>(
        OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken _)
    {
        if (context.JsonTypeInfo.Type == typeof(TEnum))
        {
            schema.Type = JsonSchemaType.String;
        }

        return Task.CompletedTask;
    }

    private static Action<OpenApiOptions> WithInlineEnums(Action<OpenApiOptions> configure)
    {
        return options =>
        {
            options.CreateSchemaReferenceId = InlineEnumReferenceId;
            configure(options);
        };
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
        Assert.True(
            names.Count == count,
            $"{path}: x-enum-varnames has {names.Count} entries, enum has {count}");

        var varNames = names.ToJsonString();
        var enumNames = schema["x-enumNames"]?.ToJsonString() ?? "(missing)";

        Assert.True(
            enumNames == varNames,
            $"{path}: x-enumNames {enumNames} differs from x-enum-varnames {varNames}");
        Assert.True(
            descriptions is null || descriptions.Count == count,
            $"{path}: x-enum-descriptions has {descriptions?.Count} entries, enum has {count}");
    }

    // Then

    // Reads a document with the reader's default rules. Tests call the reader only here, so the
    // test on a document without info covers the error list of the others.
    public static ReadResult Read(string document)
    {
        return OpenApiDocument.Parse(document, "json", new OpenApiReaderSettings());
    }

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

    // Returns the parameter called name of the GET operation at path.
    public static JsonNode Parameter(JsonNode document, string path, string name)
    {
        var parameters = document["paths"]![path]!["get"]!["parameters"]!.AsArray();

        return parameters.Single(parameter => (string?)parameter!["name"] == name)!;
    }

    // Returns the element schema of the JSON array that the POST operation at path reads.
    public static JsonNode BodyItems(JsonNode document, string path)
    {
        var body = document["paths"]![path]!["post"]!["requestBody"]!;

        return body["content"]!["application/json"]!["schema"]!["items"]!;
    }

    // Returns the status schemas at path parameter, query parameter and body element.
    public static JsonNode[] LocationSchemas(JsonNode document)
    {
        return
        [
            Parameter(document, "/path/{status}/{id}", "status")["schema"]!,
            Parameter(document, "/query", "status")["schema"]!,
            BodyItems(document, "/body"),
        ];
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

    // Returns a copy without x-enum extensions and without enum where type is "integer". A type
    // list such as ["null","integer"] keeps its enum.
    public static JsonNode WithoutAddedKeywords(JsonNode node)
    {
        var copy = node.DeepClone();

        foreach (var schema in Objects(copy).ToList())
        {
            var isInteger = schema["type"]?.ToJsonString() == "\"integer\"";
            var added = schema
                .Select(keyword => keyword.Key)
                .Where(key => key.StartsWith("x-enum", StringComparison.Ordinal)
                    || (isInteger && key == "enum"))
                .ToList();

            added.ForEach(key => schema.Remove(key));
        }

        return copy;
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

    public static void AssertOneStatusComponent(JsonNode document)
    {
        AssertSingleComponent(document, "OrderStatus");
        AssertStatusComponent(document);
    }

    // Checks that one component name starts with name; a copy such as OrderStatus2 fails it.
    public static void AssertSingleComponent(JsonNode document, string name)
    {
        Assert.Single(
            Schemas(document), entry => entry.Key.StartsWith(name, StringComparison.Ordinal));
    }

    // Checks that none of the named components carries enum or an x-enum extension.
    public static void AssertUntouched(JsonNode document, params string[] names)
    {
        Assert.All(names, name => Assert.DoesNotContain(
            Schema(document, name).AsObject(),
            property => property.Key == "enum"
                || property.Key.StartsWith("x-enum", StringComparison.Ordinal)));
    }

    // Checks that the reader kept all three lists of each named component as extensions. In 3.1
    // the reader takes a key without x- as an unrecognized keyword and reports no error.
    public static void AssertListExtensions(OpenApiDocument document, params string[] names)
    {
        Assert.All(names, name =>
        {
            var extensions = document.Components!.Schemas![name].Extensions;

            Assert.NotNull(extensions);
            Assert.Contains("x-enum-varnames", extensions);
            Assert.Contains("x-enumNames", extensions);
            Assert.Contains("x-enum-descriptions", extensions);
        });
    }
}
