using MinimalEndpoints.Annotations;
using MinimalEndpoints.EndToEnd.TestApp.Groups;

namespace MinimalEndpoints.EndToEnd.TestApp.Endpoints;

/// <summary>
/// Lives under <see cref="VersionedStoreGroup"/>, whose prefix declares a <c>{version}</c> token, so
/// it answers under <c>/store/v{version}/catalog</c> and binds that token to its <c>version</c>
/// parameter by name — no <c>[FromRoute]</c> needed. Echoes the bound value so a test can assert it.
/// </summary>
[MapGet("/catalog", Group = typeof(VersionedStoreGroup))]
public class StoreCatalogEndpoint
{
    public Task<IResult> HandleAsync(string version) =>
        Task.FromResult(Results.Ok(new { version }));
}

/// <summary>
/// Lives under <see cref="VersionedStorePageGroup"/>, so its full route is
/// <c>/store/v{version}/p{page:int}/items</c>. Binds the token from the PARENT group prefix
/// (<c>version</c>) and the constrained token from its own group prefix (<c>page</c>), proving
/// hierarchical prefix tokens compose and bind together.
/// </summary>
[MapGet("/items", Group = typeof(VersionedStorePageGroup))]
public class StorePageItemsEndpoint
{
    public Task<IResult> HandleAsync(string version, int page) =>
        Task.FromResult(Results.Ok(new { version, page }));
}
