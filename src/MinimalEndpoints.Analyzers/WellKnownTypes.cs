namespace MinimalEndpoints.Analyzers;

internal static class WellKnownTypes
{
    public const string RootNamespace = "MinimalEndpoints";
    public const string ConfigurableEndpointTypeName = "IConfigurableEndpoint";

    internal static class Annotations
    {
        public const string Namespace = RootNamespace + ".Annotations";
        public const string MapMethodsAttributeName = "MapMethodsAttribute";
        public const string MapGetAttributeName = "MapGetAttribute";
        public const string MapPostAttributeName = "MapPostAttribute";
        public const string MapPutAttributeName = "MapPutAttribute";
        public const string MapDeleteAttributeName = "MapDeleteAttribute";
        public const string MapPatchAttributeName = "MapPatchAttribute";
        public const string MapHeadAttributeName = "MapHeadAttribute";
    }
}
