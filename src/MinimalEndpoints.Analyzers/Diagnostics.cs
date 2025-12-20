using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.Analyzers;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor MissingEntryPoint = new DiagnosticDescriptor(
        id: "MINEP001",
        title: "Endpoint missing entry point method",
        messageFormat:
        "Class '{0}' is marked with MapMethodsAttribute but does not contain a valid entry point method. Expected a public instance method named 'Handle', 'HandleAsync', or the method specified in the EntryPoint property.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Endpoints must have a valid entry point method to handle requests."
    );

    public static readonly DiagnosticDescriptor MultipleAttributesDetected = new DiagnosticDescriptor(
        id: "MINEP002",
        title: "Multiple MapMethods attributes detected",
        messageFormat:
        "Class '{0}' is marked with multiple MapMethods attributes. Only one MapMethods attribute is allowed per endpoint class.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Endpoints must not have more than one MapMethods attribute."
    );

    public static readonly DiagnosticDescriptor ServiceTypeMissingEntryPoint = new DiagnosticDescriptor(
        id: "MINEP003",
        title: "ServiceType interface missing entry point method",
        messageFormat:
        "The ServiceType '{0}' specified for endpoint '{1}' does not contain the entry point method '{2}'. The interface must declare this method with a compatible signature.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When ServiceType is specified, the interface must contain the entry point method that will be called."
    );
}
