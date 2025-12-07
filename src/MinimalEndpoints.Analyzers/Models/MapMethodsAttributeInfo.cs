namespace MinimalEndpoints.Analyzers.Models;

internal class MapMethodsAttributeInfo
{
    public string Pattern { get; set; }

    public string EndpointBuilderMethodName { get; set; }

    public string[] Methods { get; set; }

    public ServiceLifetime Lifetime { get; set; }

    public string GroupPrefix { get; set; }

    public string EntryPoint { get; set; }

    public string ServiceName { get; set; }
}
