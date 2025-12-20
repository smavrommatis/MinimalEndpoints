using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.Sample.Endpoints;

[MapPost("/test")]
public class PostTest: IConfigurableEndpoint
{
    public async Task<IResult> HandleAsync()
    {
        return Results.Ok();
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .WithTags("test-group");
    }
}
