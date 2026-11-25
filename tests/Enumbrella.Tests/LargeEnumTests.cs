using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;

namespace Enumbrella.Tests;

// Runs apart from all other tests, so their load stays out of the measured times.
[CollectionDefinition(DisableParallelization = true)]
[Collection<LargeEnumTests>]
public class LargeEnumTests(ITestOutputHelper output)
{
    private const int FetchCount = 10;

    [Fact]
    public async Task AddEnumDescriptions_EnumWith5000Members_DelaysDocumentByAtMost100Ms()
    {
        // Given: a 5000-member enum with one description on two warm hosts, one of them registered
        var responseType = typeof(ValueResponse<>).MakeGenericType(CreateEnum(memberCount: 5000));
        await using var registered = await TestApi.Start(responseType, TestApi.Register);
        await using var unregistered = await TestApi.Start(responseType, _ => { });
        await TestApi.Fetch(registered);
        await TestApi.Fetch(unregistered);

        // When: each host serves its document ten times
        var (registeredTime, documents) = await TimeFetches(registered);
        var (unregisteredTime, _) = await TimeFetches(unregistered);

        // Then: TestApi.Fetch takes at most 100 ms longer on average and returns all 5000 members
        var registeredMean = registeredTime.TotalMilliseconds / FetchCount;
        var unregisteredMean = unregisteredTime.TotalMilliseconds / FetchCount;
        var added = registeredMean - unregisteredMean;
        var message = FormatMeans(registeredMean, unregisteredMean, added);
        output.WriteLine(message);

        Assert.True(added <= 100, message);
        Assert.All(documents, document => AssertEveryMember(document, memberCount: 5000));
    }

    private static Type CreateEnum(int memberCount)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("LargeEnums"), AssemblyBuilderAccess.Run);
        var builder = assembly.DefineDynamicModule("LargeEnums")
            .DefineEnum("LargeEnum", TypeAttributes.Public, typeof(int));
        var description = new CustomAttributeBuilder(
            typeof(DescriptionAttribute).GetConstructor([typeof(string)])!, ["First member"]);

        builder.DefineLiteral("Member0", 0).SetCustomAttribute(description);

        for (var value = 1; value < memberCount; value++)
        {
            builder.DefineLiteral($"Member{value}", value);
        }

        return builder.CreateType();
    }

    private static async Task<(TimeSpan Time, List<JsonNode> Documents)> TimeFetches(
        WebApplication host)
    {
        var documents = new List<JsonNode>();
        var start = Stopwatch.GetTimestamp();

        while (documents.Count < FetchCount)
        {
            documents.Add(await TestApi.Fetch(host));
        }

        return (Stopwatch.GetElapsedTime(start), documents);
    }

    private static string FormatMeans(double registered, double unregistered, double added)
    {
        return FormattableString.Invariant(
            $"With {registered:F1} ms, without {unregistered:F1} ms, difference {added:F1} ms");
    }

    // Counts the expected entries up from zero, because 5000 of them make an unreadable literal.
    private static void AssertEveryMember(JsonNode document, int memberCount)
    {
        var values = Enumerable.Range(0, memberCount).ToList();
        string[] descriptions = ["First member", .. Enumerable.Repeat("", memberCount - 1)];

        TestApi.AssertEnum(
            TestApi.Schema(document, "LargeEnum"),
            JsonSerializer.Serialize(values),
            JsonSerializer.Serialize(values.Select(value => $"Member{value}")),
            JsonSerializer.Serialize(descriptions));
    }
}
