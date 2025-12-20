using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.AdvancedSample.Groups;

/// <summary>
/// Defines the API V1 endpoint group with shared configuration.
/// </summary>
[MapGroup("/products", GroupName = "Products", ParentGroup = typeof(ApiV1Group))]
public class ProductsGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
    }
}
