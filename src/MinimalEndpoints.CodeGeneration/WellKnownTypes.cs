namespace MinimalEndpoints.CodeGeneration;

internal static class WellKnownTypes
{
    public const string RootNamespace = "MinimalEndpoints";
    public const string ConfigurableEndpointTypeName = "IConfigurableEndpoint";
    public const string ConfigurableGroupTypeName = "IConfigurableGroup";
    public const string ConditionalMappingTypeName = "IConditionallyMapped";

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

        public const string MapGroupAttributeName = "MapGroupAttribute";

        // Fully-qualified metadata names for ForAttributeWithMetadataName, which pre-indexes
        // attribute names per compilation. One provider is registered per name in the generator.
        public const string MapMethodsAttributeFullName = Namespace + "." + MapMethodsAttributeName;
        public const string MapGetAttributeFullName = Namespace + "." + MapGetAttributeName;
        public const string MapPostAttributeFullName = Namespace + "." + MapPostAttributeName;
        public const string MapPutAttributeFullName = Namespace + "." + MapPutAttributeName;
        public const string MapDeleteAttributeFullName = Namespace + "." + MapDeleteAttributeName;
        public const string MapPatchAttributeFullName = Namespace + "." + MapPatchAttributeName;
        public const string MapHeadAttributeFullName = Namespace + "." + MapHeadAttributeName;
        public const string MapGroupAttributeFullName = Namespace + "." + MapGroupAttributeName;

        /// <summary>
        /// Every endpoint mapping attribute plus the group attribute, by fully-qualified metadata
        /// name. The generator registers one <c>ForAttributeWithMetadataName</c> provider per entry.
        /// </summary>
        public static readonly string[] AllMapAttributeMetadataNames =
        {
            MapGetAttributeFullName,
            MapPostAttributeFullName,
            MapPutAttributeFullName,
            MapDeleteAttributeFullName,
            MapPatchAttributeFullName,
            MapHeadAttributeFullName,
            MapMethodsAttributeFullName,
            MapGroupAttributeFullName
        };
    }
}
