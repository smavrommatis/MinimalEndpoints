using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.CodeGeneration;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor MissingEntryPoint = new DiagnosticDescriptor(
        id: "MINEP001",
        title: "Endpoint missing entry point method",
        messageFormat:
        "Class '{0}' is marked with {1} but does not contain a valid entry point method. " +
        "Add a public instance method named 'Handle', 'HandleAsync' or specify a custom method name using the EntryPoint property.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Endpoints must have a valid entry point method to handle requests. " +
        "The method should be public, non-static, and return a value. " +
        "Example: public async Task<IResult> HandleAsync() { return Results.Ok(); }.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP001.md"
    );

    public static readonly DiagnosticDescriptor MultipleAttributesDetected = new DiagnosticDescriptor(
        id: "MINEP002",
        title: "Multiple Map attributes detected",
        messageFormat:
        "Class '{0}' is marked with multiple Map attributes. Only one Map attribute is allowed per endpoint class. " +
        "Remove duplicate attributes or use MapMethodsAttribute with an array of HTTP methods.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Endpoints must not have more than one Map attribute. " +
        "If you need to handle multiple HTTP methods, use [MapMethods(\"/route\", new[] { \"GET\", \"POST\" })].",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP002.md"
    );

    public static readonly DiagnosticDescriptor ServiceTypeMissingEntryPoint = new DiagnosticDescriptor(
        id: "MINEP003",
        title: "ServiceType interface missing entry point method",
        messageFormat:
        "The ServiceType '{0}' specified for endpoint '{1}' does not contain the entry point method '{2}'. " +
        "Add the method to the interface or change the ServiceType property.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When ServiceType is specified, the interface must contain the entry point method that will be called. " +
        "The interface method should match the signature of the endpoint's entry point. " +
        "Example: public interface IMyEndpoint { Task<IResult> HandleAsync(); }.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP003.md"
    );

    public static readonly DiagnosticDescriptor AmbiguousRoutes = new DiagnosticDescriptor(
        id: "MINEP004",
        title: "Ambiguous route pattern detected",
        messageFormat:
        "Endpoint '{0}' has route pattern '{1}' that conflicts with endpoint '{2}'. " +
        "Multiple endpoints with the same HTTP method and route pattern will cause routing ambiguity.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Multiple endpoints should not have identical route patterns for the same HTTP method. " +
        "This will cause routing ambiguity and unpredictable behavior at runtime. " +
        "Consider using different route patterns, route constraints, or consolidating the endpoints. " +
        "Route parameters are compared by position only — routes that differ solely by parameter name " +
        "or route constraint (e.g. '{id:int}' vs '{id:alpha}') are conservatively reported as conflicts.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP004.md",
        customTags: WellKnownDiagnosticTags.CompilationEnd
    );

    public static readonly DiagnosticDescriptor InvalidGroupType = new DiagnosticDescriptor(
        id: "MINEP005",
        title: "Invalid group type",
        messageFormat:
        "The type '{0}' referenced as a group by '{1}' is not decorated with MapGroupAttribute. " +
        "Ensure the group type has the [MapGroup] attribute.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A type used as an endpoint's Group or a group's ParentGroup must be decorated with " +
        "MapGroupAttribute. Example: [MapGroup(\"/api/v1\")] public class ApiV1Group { }.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP005.md"
    );

    public static readonly DiagnosticDescriptor CyclicGroupHierarchy = new DiagnosticDescriptor(
        id: "MINEP006",
        title: "Cyclic group hierarchy detected",
        messageFormat:
        "Group '{0}' has a cyclic hierarchy: {1}. " +
        "Group hierarchies must not contain cycles.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Group hierarchies must be acyclic. A group cannot directly or indirectly reference itself as a parent. " +
        "Example of invalid hierarchy: GroupA -> GroupB -> GroupC -> GroupA (cycle). " +
        "Ensure each group's ParentGroup property forms a proper tree structure without cycles.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP006.md",
        customTags: WellKnownDiagnosticTags.CompilationEnd
    );

    public static readonly DiagnosticDescriptor InvalidSymbolKind = new DiagnosticDescriptor(
        id: "MINEP007",
        title: "A class can either be an Endpoint or a Group, not both",
        messageFormat:
        "The type '{0}' is marked as both an Endpoint and a Group. " +
        "A class cannot be decorated with both MapGroupAttribute and a Map* endpoint attribute.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A class cannot serve as both an endpoint and a group. " +
        "Ensure that classes intended to define endpoint groups are only decorated with MapGroupAttribute, "+
        "and classes intended to define endpoints are only decorated with Map* attributes.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP007.md"
    );

    public static readonly DiagnosticDescriptor UnsupportedEndpointShape = new DiagnosticDescriptor(
        id: "MINEP008",
        title: "Endpoint or group class has an unsupported shape",
        messageFormat:
        "Type '{0}' is marked with a MinimalEndpoints attribute but cannot be mapped because it is {1}. " +
        "Endpoint and group classes must be non-generic, accessible (at least internal), non-file-local classes.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The source generator must be able to reference the endpoint or group class by name and " +
        "register it for dependency injection. Open generic types, file-local types, and types whose effective " +
        "accessibility is below internal cannot be referenced from the generated code, so they are skipped. " +
        "Make the class a non-generic, at-least-internal, non-file-local class to map it.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP008.md"
    );

    public static readonly DiagnosticDescriptor CrossAssemblyGroupNotScanned = new DiagnosticDescriptor(
        id: "MINEP009",
        title: "Referenced group is not scanned",
        messageFormat:
        "'{0}' references group '{1}' from referenced assembly '{2}', which is not scanned for endpoints. " +
        "The group's route prefix and configuration will not be applied — '{0}' is mapped without the group. " +
        "Add [assembly: ScanReferencedEndpoints] to the host (optionally targeting it with " +
        "[assembly: ScanReferencedEndpoints(typeof(...))]).",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When an endpoint's Group (or a group's ParentGroup) refers to a public MapGroup type in a " +
        "referenced assembly that cross-assembly scanning does not cover, the group is never discovered, so it " +
        "cannot be composed: the endpoint is silently mapped without the group's route prefix and configuration. " +
        "Enable scanning of that assembly with [assembly: ScanReferencedEndpoints], optionally targeting it by type.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP009.md"
    );

    public static readonly DiagnosticDescriptor GenericEntryPoint = new DiagnosticDescriptor(
        id: "MINEP010",
        title: "Entry point method must not be generic",
        messageFormat:
        "The entry point method '{0}' on endpoint '{1}' is generic. The generated handler cannot supply type " +
        "arguments, so a generic entry point cannot be mapped. Make the entry point a non-generic method.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An endpoint's entry point method must be non-generic. ASP.NET Core cannot infer or " +
        "supply type arguments for the generated route handler delegate.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP010.md"
    );

    public static readonly DiagnosticDescriptor UnsupportedParameterModifier = new DiagnosticDescriptor(
        id: "MINEP011",
        title: "Entry point parameter uses an unsupported modifier",
        messageFormat:
        "Parameter '{0}' of entry point '{1}' on endpoint '{2}' uses a 'ref', 'out', 'in', or pointer modifier. " +
        "ASP.NET Core cannot model-bind such a parameter and the generated handler cannot pass it; remove the modifier.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Entry point parameters must be passed by value. By-reference (ref/out/in) and pointer " +
        "parameters cannot be model-bound by ASP.NET Core or reproduced by the generated handler delegate.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP011.md"
    );

    public static readonly DiagnosticDescriptor EndpointNotAssignableToServiceType = new DiagnosticDescriptor(
        id: "MINEP012",
        title: "Endpoint is not assignable to its ServiceType",
        messageFormat:
        "Endpoint '{1}' specifies ServiceType '{0}' but does not implement or inherit it. The generated " +
        "registration would not compile. Implement '{0}' on '{1}' or change the ServiceType.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When ServiceType is specified, the endpoint class must be assignable to it (implement the " +
        "interface or derive from the base type), because it is registered as the implementation of that service.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP012.md"
    );

    public static readonly DiagnosticDescriptor DuplicateServiceType = new DiagnosticDescriptor(
        id: "MINEP013",
        title: "Multiple endpoints register the same ServiceType",
        messageFormat:
        "Endpoints '{0}' and '{1}' both register ServiceType '{2}'. The DI container resolves only the last " +
        "registration, so one endpoint's route will run the other endpoint's class. Use a distinct ServiceType per endpoint.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each endpoint registered via ServiceType should use a distinct service type. When two " +
        "endpoints register the same ServiceType, the last DI registration wins and both handlers resolve the same instance.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP013.md",
        customTags: WellKnownDiagnosticTags.CompilationEnd
    );

    public static readonly DiagnosticDescriptor GroupShapeNotMapped = new DiagnosticDescriptor(
        id: "MINEP014",
        title: "Group cannot be applied because of its shape",
        messageFormat:
        "Endpoint '{0}' references group '{1}', but '{1}' has an unsupported shape ({2}) and is not mapped. " +
        "'{0}' is therefore registered without the group's route prefix and configuration.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A group that is abstract, generic, file-local, or insufficiently accessible is never mapped, " +
        "so endpoints referencing it silently lose the group's route prefix and configuration. Give the group a supported shape.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP014.md"
    );

    public static readonly DiagnosticDescriptor MalformedEndpointAttribute = new DiagnosticDescriptor(
        id: "MINEP015",
        title: "Endpoint has a malformed Map attribute",
        messageFormat:
        "Endpoint '{0}' cannot be mapped because {1}, so it is skipped. Fix the Map attribute to map the endpoint.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An endpoint's Map attribute must specify a non-null route pattern and, for [MapMethods], " +
        "at least one HTTP method. An empty (or all-null) method array or a null pattern yields no routable " +
        "endpoint, so the generator declines it. Provide a valid pattern and at least one HTTP method, or use a " +
        "verb-specific attribute such as [MapGet] or [MapPost].",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP015.md"
    );

    /// <summary>
    /// Reported by the GENERATOR (not an analyzer) when the output step throws unexpectedly. It
    /// turns an opaque CS8785 ("generator failed to generate source") into an actionable build
    /// error that names the failure, so generator bugs are visible at build time instead of
    /// silently producing no output. Because it is generator-reported and not in any analyzer's
    /// SupportedDiagnostics, it is intentionally not part of analyzer release tracking.
    /// </summary>
    // RS2000 flags rules missing from analyzer release tracking. MINEP999 is generator-reported and
    // intentionally NOT in any analyzer's SupportedDiagnostics (see remarks above), so adding it to
    // AnalyzerReleases.*.md would be incorrect — suppress the rule locally for this one descriptor.
#pragma warning disable RS2000
    public static readonly DiagnosticDescriptor GeneratorFailure = new DiagnosticDescriptor(
        id: "MINEP999",
        title: "MinimalEndpoints source generator failed",
        messageFormat:
        "The MinimalEndpoints source generator failed unexpectedly and produced no output: {0}. " +
        "This is a bug in the generator — please report it with the details above.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An unexpected exception in the generator's output step is caught and surfaced as this " +
        "diagnostic instead of the opaque CS8785, so the failure is visible and actionable at build time.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/blob/main/docs/diagnostics/MINEP999.md"
    );
#pragma warning restore RS2000
}
