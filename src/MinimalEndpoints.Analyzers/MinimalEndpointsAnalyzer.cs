using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using MinimalEndpoints.Analyzers.Utilities;

namespace MinimalEndpoints.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MinimalEndpointsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        Diagnostics.MissingEntryPoint,
        Diagnostics.MultipleAttributesDetected
    ];

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

        var attributes = namedTypeSymbol.GetMapMethodAttributes();
        if (attributes.Length == 0)
        {
            return;
        }

        if (attributes.Length > 1)
        {
            var multipleAttributesDiagnostic = Diagnostic.Create(
                Diagnostics.MultipleAttributesDetected,
                classDeclaration.Identifier.GetLocation(),
                namedTypeSymbol.Name
            );

            context.ReportDiagnostic(multipleAttributesDiagnostic);
            return;
        }

        var mapMethodsAttributeDefinition = attributes[0].GetMapMethodAttributeDefinition();
        var entryPoint = namedTypeSymbol.FindEntryPointMethod(mapMethodsAttributeDefinition.EntryPoint);

        if (entryPoint == null)
        {
            var missingEntryPointDiagnostic = Diagnostic.Create(
                Diagnostics.MissingEntryPoint,
                classDeclaration.Identifier.GetLocation(),
                namedTypeSymbol.Name
            );

            context.ReportDiagnostic(missingEntryPointDiagnostic);
        }
    }
}
