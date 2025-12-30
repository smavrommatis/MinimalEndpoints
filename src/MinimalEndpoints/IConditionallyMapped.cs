using Microsoft.AspNetCore.Builder;

namespace MinimalEndpoints;

/// <summary>
/// Define a contract for endpoints and or groups that support conditional mapping.
/// </summary>
public interface IConditionallyMapped
{
    static abstract bool ShouldMap(IApplicationBuilder app);
}
