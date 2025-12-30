using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;
using MinimalEndpoints.CodeGeneration.Groups.Models;

namespace MinimalEndpoints.CodeGeneration.Endpoints.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EndpointsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        Diagnostics.MissingEntryPoint,
        Diagnostics.MultipleAttributesDetected,
        Diagnostics.ServiceTypeMissingEntryPoint,
        Diagnostics.InvalidGroupType
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

        var attributes = namedTypeSymbol.GetAttributes()
            .Where(EndpointDefinition.Factory.Predicate)
            .ToArray();

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
                namedTypeSymbol.Name,
                mapMethodsAttributeDefinition.EntryPoint
            );

            context.ReportDiagnostic(missingEntryPointDiagnostic);
            return;
        }

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

        // Validate Group if specified
        var groupTypeSymbol = GetGroupTypeSymbol(attributes[0]);
        if (groupTypeSymbol != null)
        {
            ValidateGroupType(context, classDeclaration, namedTypeSymbol, groupTypeSymbol);
        }
    }

    private static void ValidateGroupType(
        SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDeclaration,
        INamedTypeSymbol endpointSymbol,
        INamedTypeSymbol groupTypeSymbol)
    {
        // Check if group type has MapGroupAttribute
        var mapGroupAttribute = groupTypeSymbol.GetAttributes()
            .SingleOrDefault(EndpointGroupDefinition.Factory.Predicate);

        if (mapGroupAttribute == null)
        {
            var diagnostic = Diagnostic.Create(
                Diagnostics.InvalidGroupType,
                classDeclaration.Identifier.GetLocation(),
                groupTypeSymbol.Name,
                endpointSymbol.Name
            );

            context.ReportDiagnostic(diagnostic);
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

    private static INamedTypeSymbol GetGroupTypeSymbol(AttributeData attributeData)
    {
        var groupArg = attributeData.NamedArguments
            .FirstOrDefault(arg => arg.Key == "Group");

        if (groupArg.Value.Value is INamedTypeSymbol groupType)
        {
            return groupType;
        }

        return null;
    }
}
