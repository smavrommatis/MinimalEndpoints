using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MinimalEndpoints.EndToEnd.Tests;

/// <summary>
/// True end-to-end tests: boots the generated <c>AddMinimalEndpoints()</c>/
/// <c>UseMinimalEndpoints()</c> wiring in an in-process server and exercises real HTTP, so
/// wrong verbs, dropped group prefixes, mis-applied lifetimes, skipped conditional mappings,
/// and un-invoked configure hooks fail here even though they compile and pass text assertions.
/// </summary>
public class EndToEndHttpTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndToEndHttpTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task MapGet_Route_Returns200WithBody()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/ping");

        response.EnsureSuccessStatusCode();
        Assert.Contains("pong", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MapPost_Body_RoundTrips()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/echo", new { message = "hello-roundtrip" });

        response.EnsureSuccessStatusCode();
        Assert.Contains("hello-roundtrip", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GroupedEndpoint_RespondsUnderFullPrefix()
    {
        var client = _factory.CreateClient();

        var grouped = await client.GetAsync("/api/v1/grouped");
        Assert.Equal(HttpStatusCode.OK, grouped.StatusCode);

        // The unprefixed path must not be mapped.
        var unprefixed = await client.GetAsync("/grouped");
        Assert.Equal(HttpStatusCode.NotFound, unprefixed.StatusCode);
    }

    [Fact]
    public async Task GroupConfigureHook_IsApplied()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/grouped");

        response.EnsureSuccessStatusCode();
        Assert.Equal("v1", Assert.Single(response.Headers.GetValues("X-Group")));
    }

    [Fact]
    public async Task Lifetimes_ResolveAsDeclared()
    {
        var client = _factory.CreateClient();

        // Singleton: one instance for the whole app, so the counter accumulates across requests.
        var firstSingleton = await ReadCountAsync(client, "/lifetime/singleton");
        var secondSingleton = await ReadCountAsync(client, "/lifetime/singleton");
        Assert.True(
            secondSingleton > firstSingleton,
            $"Singleton counter should accumulate across requests (saw {firstSingleton} then {secondSingleton}).");

        // Transient: a fresh instance per request, so the counter always reads 1.
        Assert.Equal(1, await ReadCountAsync(client, "/lifetime/transient"));
        Assert.Equal(1, await ReadCountAsync(client, "/lifetime/transient"));
    }

    [Fact]
    public async Task ConditionalEndpoint_ShouldMapTrue_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/conditional");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConditionalEndpoint_ShouldMapFalse_Returns404()
    {
        var client = _factory
            .WithWebHostBuilder(b => b.UseSetting("Features:ConditionalEndpoint", "false"))
            .CreateClient();

        var response = await client.GetAsync("/conditional");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ConfigureHook_IsApplied()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/configured");

        response.EnsureSuccessStatusCode();
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("X-Configured")));
    }

    [Fact]
    public async Task ReferencedLibraryEndpoint_RespondsUnderComposedPrefix()
    {
        var client = _factory.CreateClient();

        // Endpoint defined in a referenced COMPILED assembly, in a referenced group → /lib/ping.
        var response = await client.GetAsync("/lib/ping");

        response.EnsureSuccessStatusCode();
        Assert.Contains("referenced-library", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ReferencedGroupConfigureHook_IsApplied()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/lib/ping");

        response.EnsureSuccessStatusCode();
        // Proves IConfigurableGroup.ConfigureGroup on a REFERENCED group is invoked across assemblies.
        Assert.Equal("lib", Assert.Single(response.Headers.GetValues("X-Lib-Group")));
    }

    [Fact]
    public async Task HostEndpointInReferencedGroup_RespondsUnderComposedPrefix()
    {
        var client = _factory.CreateClient();

        // A HOST endpoint with Group = typeof(referenced LibGroup): cross-assembly group composition.
        var response = await client.GetAsync("/lib/host-in-lib");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("host-in-referenced-group", await response.Content.ReadAsStringAsync());

        // It is only mapped under the referenced group's prefix, never at the unprefixed path.
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/host-in-lib")).StatusCode);
    }

    [Fact]
    public async Task MapPut_RouteIdAndBody_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/resource/abc", new { value = "v1" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("abc", body);
        Assert.Contains("PUT", body);
        Assert.Contains("v1", body);
    }

    [Fact]
    public async Task MapPatch_RouteIdAndBody_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync("/resource/abc", new { value = "v1" });

        response.EnsureSuccessStatusCode();
        Assert.Contains("PATCH", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MapDelete_Returns204()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/resource/abc");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task MapHead_EmittedViaMapMethods_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/resource"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerbEndpoint_WrongVerb_Returns405()
    {
        var client = _factory.CreateClient();

        // /resource/{id} is mapped for PUT/PATCH/DELETE only, so a GET to that template is a method
        // mismatch (the route exists, the verb does not) → 405, not 404.
        var response = await client.GetAsync("/resource/abc");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task ScopedLifetime_ResolvesPerRequest()
    {
        var client = _factory.CreateClient();

        // Scoped = a fresh instance per request scope, so the per-instance counter reads 1 each request.
        Assert.Equal(1, await ReadCountAsync(client, "/lifetime/scoped"));
        Assert.Equal(1, await ReadCountAsync(client, "/lifetime/scoped"));
    }

    private static async Task<int> ReadCountAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }
}
