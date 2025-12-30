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
        description: "Endpoints must have a valid entry point method to handle requests." +
        "The method should be public, non-static, and return a value." +
        "Example: public async Task<IResult> HandleAsync() { return Results.Ok(); }.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/docs/MINEP001.md"
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
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/docs/MINEP002.md"
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
        description: "When ServiceType is specified, the interface must contain the entry point method that will be called." +
        "The interface method should match the signature of the endpoint's entry point." +
        "Example: public interface IMyEndpoint { Task<IResult> HandleAsync(); }.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/docs/diagnostics/MINEP003.md"
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
        "Consider using different route patterns, route constraints, or consolidating the endpoints.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/docs/diagnostics/MINEP004.md",
        customTags: WellKnownDiagnosticTags.CompilationEnd
    );

    public static readonly DiagnosticDescriptor InvalidGroupType = new DiagnosticDescriptor(
        id: "MINEP005",
        title: "Invalid endpoint group type",
        messageFormat:
        "The Group type '{0}' specified for endpoint '{1}' is not decorated with MapGroupAttribute. " +
        "Ensure the group has the [MapGroup] attribute.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Endpoint groups must decorated with MapGroupAttribute." +
        "Example: [MapGroup(\"/api/v1\")] public class ApiV1Group { }.",
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/docs/diagnostics/MINEP005.md"
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
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/docs/diagnostics/MINEP006.md",
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
        helpLinkUri: "https://github.com/smavrommatis/MinimalEndpoints/docs/diagnostics/MINEP007.md"
    );
}
