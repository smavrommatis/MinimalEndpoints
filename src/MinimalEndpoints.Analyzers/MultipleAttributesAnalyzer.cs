using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using MinimalEndpoints.Analyzers.Utilities;

namespace MinimalEndpoints.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MultipleAttributesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
        id: Diagnostics.MultipleAttributesDetectedId,
        title: "Multiple MapMethods attributes detected",
        messageFormat:
        "Class '{0}' is marked with multiple MapMethods attributes. Only one MapMethods attribute is allowed per endpoint class.",
        category: "MinimalEndpoints",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Endpoints must not have more than one MapMethods attribute.");

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

        var attributes = namedTypeSymbol.GetMapMethodsAttributes();
        if (attributes.Length <= 1)
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
