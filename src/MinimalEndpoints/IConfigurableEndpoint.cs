using Microsoft.AspNetCore.Builder;

namespace MinimalEndpoints;

public interface IConfigurableEndpoint
{
    static abstract void Configure(IApplicationBuilder app, IEndpointConventionBuilder endpoint);
}
