using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MinimalEndpoints.EndToEnd.Tests;

/// <summary>
/// Verifies that route tokens declared in a group's prefix — e.g. <c>[MapGroup("/store/v{version}")]</c> —
/// bind into endpoint handler parameters by name with NO generator change. The generator emits the prefix
/// verbatim into <c>MapGroup(...)</c> and forwards handler parameters as declared, so ASP.NET's standard
/// route-value-to-parameter binding does the rest. Covers a single-level token, parent+child token
/// composition across a group hierarchy, and a route constraint declared inside a prefix token.
/// </summary>
public class GroupPrefixRouteTokenTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GroupPrefixRouteTokenTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TokenInGroupPrefix_BindsToHandlerParameter()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/store/v2025/catalog");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"version\":\"2025\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TokenSegmentInGroupPrefix_IsRequired()
    {
        var client = _factory.CreateClient();

        // The {version} segment is part of the route template, so the path without it must not match.
        var response = await client.GetAsync("/store/catalog");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TokensInParentAndChildGroupPrefixes_BothBind()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/store/v2025/p7/items");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"version\":\"2025\"", body);
        Assert.Contains("\"page\":7", body);
    }

    [Fact]
    public async Task ConstrainedTokenInGroupPrefix_RejectsNonMatchingSegment()
    {
        var client = _factory.CreateClient();

        // The child prefix token is {page:int}; a non-integer segment fails the constraint -> no match.
        var response = await client.GetAsync("/store/v2025/pNaN/items");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
