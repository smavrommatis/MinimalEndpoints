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
}
