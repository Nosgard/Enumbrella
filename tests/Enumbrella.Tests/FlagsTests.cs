namespace Enumbrella.Tests;

public class FlagsTests
{
    [Fact]
    public async Task AddEnumDescriptions_FlagsAtEveryLocation_MatchesUnregisteredDocument()
    {
        // Given: [Flags] Permissions as property, nullable property, path parameter, query
        // parameter and body element, in one host with the registration and one without
        await using var registeredHost =
            await TestApi.StartLocations<PermissionsResponse, Permissions>(TestApi.Register);
        await using var unregisteredHost =
            await TestApi.StartLocations<PermissionsResponse, Permissions>(_ => { });

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: both documents match character for character
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_InlineFlagsAtEveryLocation_MatchesUnregisteredDocument()
    {
        // Given: [Flags] Permissions inline as property, nullable property, path parameter, query
        // parameter and body element, in one host with the registration and one without
        await using var registeredHost =
            await TestApi.StartInlineLocations<PermissionsResponse, Permissions>(TestApi.Register);
        await using var unregisteredHost =
            await TestApi.StartInlineLocations<PermissionsResponse, Permissions>(_ => { });

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: both documents match character for character
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_StringFlagsFromOptions_MatchesUnregisteredDocument()
    {
        // Given: [Flags] Permissions as strings through the converter in the JSON options, at
        // every location, in one host with the registration and one without
        await using var registeredHost =
            await TestApi.StartLocations<PermissionsResponse, Permissions>(
                TestApi.Register, TestApi.StringEnums);
        await using var unregisteredHost =
            await TestApi.StartLocations<PermissionsResponse, Permissions>(
                _ => { }, TestApi.StringEnums);

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: both documents match character for character
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_StringFlagsFromType_MatchesUnregisteredDocument()
    {
        // Given: the [Flags] StringPermissions with the string converter on its type, as a
        // property, in one host with the registration and one without
        await using var registeredHost =
            await TestApi.Start<StringPermissionsResponse>(TestApi.Register);
        await using var unregisteredHost = await TestApi.Start<StringPermissionsResponse>(_ => { });

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: both documents match character for character
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_StringFlagsFromProperty_MatchesUnregisteredDocument()
    {
        // Given: a Permissions property with the string converter on the property, in one host
        // with the registration and one without
        await using var registeredHost =
            await TestApi.Start<ConvertedPermissionsResponse>(TestApi.Register);
        await using var unregisteredHost =
            await TestApi.Start<ConvertedPermissionsResponse>(_ => { });

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: both documents match character for character
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
    }

    [Fact]
    public async Task AddEnumDescriptions_FlagsWithForeignValues_MatchesUnregisteredDocument()
    {
        // Given: a schema transformer that writes [1,2] into the enum of [Flags] Permissions, in
        // one host with the registration after it and one without
        await using var registeredHost = await TestApi.Start<PermissionsResponse>(
            TestApi.WithEntries<Permissions>("[1,2]", TestApi.Register));
        await using var unregisteredHost = await TestApi.Start<PermissionsResponse>(
            TestApi.WithEntries<Permissions>("[1,2]", _ => { }));

        // When: both documents are generated
        var registered = await TestApi.Fetch(registeredHost);
        var unregistered = await TestApi.Fetch(unregisteredHost);

        // Then: the enum keeps the foreign [1,2], and both documents match character for character
        Assert.Equal("[1,2]", TestApi.Schema(unregistered, "Permissions")["enum"]!.ToJsonString());
        Assert.Equal(unregistered.ToJsonString(), registered.ToJsonString());
    }
}
