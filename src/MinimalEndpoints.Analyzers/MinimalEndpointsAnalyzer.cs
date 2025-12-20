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
        Diagnostics.MultipleAttributesDetected,
        Diagnostics.ServiceTypeMissingEntryPoint
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
            return;
        }

        // Validate ServiceType if specified
        if (!string.IsNullOrEmpty(mapMethodsAttributeDefinition.ServiceName))
        {
            var serviceTypeSymbol = GetServiceTypeSymbol(attributes[0]);
            if (serviceTypeSymbol != null)
            {
                var interfaceHasMethod = serviceTypeSymbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Any(m => m.Name == entryPoint.Name &&
                              !m.IsStatic &&
                              m.DeclaredAccessibility == Accessibility.Public);

                if (!interfaceHasMethod)
                {
                    var serviceTypeDiagnostic = Diagnostic.Create(
                        Diagnostics.ServiceTypeMissingEntryPoint,
                        classDeclaration.Identifier.GetLocation(),
                        serviceTypeSymbol.Name,
                        namedTypeSymbol.Name,
                        entryPoint.Name
                    );

                    context.ReportDiagnostic(serviceTypeDiagnostic);
                }
            }
        }
    }

    private static INamedTypeSymbol GetServiceTypeSymbol(AttributeData attributeData)
    {
        var serviceTypeArg = attributeData.NamedArguments
            .FirstOrDefault(arg => arg.Key == "ServiceType");

        if (serviceTypeArg.Value.Value is INamedTypeSymbol serviceType)
        {
            return serviceType;
        }

        return null;
    }
}
