using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using MinimalEndpoints.Analyzers;
using MinimalEndpoints.Analyzers.Utilities;

namespace MinimalEndpoints.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EntryPointAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
        id: Diagnostics.EndpointMissingEntryPointId,
        title: "Endpoint missing entry point method",
        messageFormat:
        "Class '{0}' is marked with MapMethodsAttribute but does not contain a valid entry point method. Expected a public instance method named 'Handle', 'HandleAsync', or the method specified in the EntryPoint property.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Endpoints must have a valid entry point method to handle requests.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Skip abstract classes
        if (classDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword))
        {
            return;
        }

        var classSymbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDeclaration);
        if (classSymbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        var mapMethodsAttributeInfo = namedTypeSymbol.GetMapMethodsAttributeInfo();
        if (mapMethodsAttributeInfo == null)
        {
            return;
        }

        // Check for entry point
        var entryPoint = namedTypeSymbol.FindEntryPointMethod(mapMethodsAttributeInfo.EntryPoint);


        if (entryPoint != null)
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            s_rule,
            classDeclaration.Identifier.GetLocation(),
            namedTypeSymbol.Name);

        context.ReportDiagnostic(diagnostic);
    }
}
