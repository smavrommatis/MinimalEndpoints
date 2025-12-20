using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.Analyzers;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor MissingEntryPoint = new DiagnosticDescriptor(
        id: "MINEP001",
        title: "Endpoint missing entry point method",
        messageFormat:
        "Class '{0}' is marked with MapMethodsAttribute but does not contain a valid entry point method. " +
        "Add a public instance method named 'Handle', 'HandleAsync', or specify a custom method name using the EntryPoint property.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Endpoints must have a valid entry point method to handle requests." +
        "The method should be public, non-static, and return IResult or Task<IResult>." +
        "Example: public async Task<IResult> HandleAsync() { return Results.Ok(); }.",
        helpLinkUri: "https://github.com/yourusername/MinimalEndpoints/docs/MINEP001.md"
    );

    public static readonly DiagnosticDescriptor MultipleAttributesDetected = new DiagnosticDescriptor(
        id: "MINEP002",
        title: "Multiple MapMethods attributes detected",
        messageFormat:
        "Class '{0}' is marked with multiple MapMethods attributes. Only one MapMethods attribute is allowed per endpoint class. " +
        "Remove duplicate attributes or use MapMethodsAttribute with an array of HTTP methods.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Endpoints must not have more than one MapMethods attribute. " +
        "If you need to handle multiple HTTP methods, use [MapMethods(\"/route\", new[] { \"GET\", \"POST\" })].",
        helpLinkUri: "https://github.com/yourusername/MinimalEndpoints/docs/MINEP002.md"
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
        helpLinkUri: "https://github.com/yourusername/MinimalEndpoints/docs/MINEP003.md"
    );
}
