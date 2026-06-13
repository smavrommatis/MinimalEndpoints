using Microsoft.AspNetCore.Builder;

namespace MinimalEndpoints;

/// <summary>
/// Define a contract for endpoints and/or groups that support conditional mapping.
/// </summary>
/// <remarks>
/// <see cref="ShouldMap"/> is <see langword="static"/> — like
/// <see cref="IConfigurableEndpoint.Configure"/> and <see cref="IConfigurableGroup.ConfigureGroup"/> —
/// so the generated code invokes it without registering or resolving an instance. Static abstract
/// interface members require C# 11 / .NET 7 or later.
/// </remarks>
public interface IConditionallyMapped
{
    /// <summary>
    /// Decides whether this endpoint or group is mapped for the current application.
    /// </summary>
    /// <param name="app">
    /// The application builder, used to inspect configuration or services (e.g. the environment or
    /// a feature flag) when deciding whether to map.
    /// </param>
    /// <returns>
    /// <see langword="true"/> to map the endpoint or group; <see langword="false"/> to skip it.
    /// Skipping a group also skips its child groups and their endpoints.
    /// </returns>
    /// <remarks>This method is evaluated once during application startup, while endpoints are mapped.</remarks>
    static abstract bool ShouldMap(IApplicationBuilder app);
}
