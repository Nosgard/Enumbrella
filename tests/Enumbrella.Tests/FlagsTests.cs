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
}
