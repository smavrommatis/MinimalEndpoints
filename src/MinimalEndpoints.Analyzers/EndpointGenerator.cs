using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MinimalEndpoints.Analyzers.Models;
using MinimalEndpoints.Analyzers.Utilities;

namespace MinimalEndpoints.Analyzers;

[Generator]
public class EndpointGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register a syntax provider that:
        // 1. Filters syntax nodes quickly (predicate) - runs on every keystroke
        // 2. Transforms matching nodes with semantic info (transform) - more expensive
        var endpointClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                // Predicate: Fast syntax-only check (no semantic model)
                predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax classDeclarationSyntax
                                                     && !classDeclarationSyntax.Modifiers.Any(SyntaxKind
                                                         .AbstractKeyword)
                                                     && classDeclarationSyntax.AttributeLists.Count > 0,
                // Transform: Slower semantic analysis with full type information
                transform: GetEndpointInfo)
            .Where(static info => info is not null); // Filter out nulls

        // Collect all endpoint infos and generate code when compilation changes
        context.RegisterSourceOutput(
            endpointClasses.Collect(),
            static (sourceContext, endpointInfos) => { GenerateEndpointExtensions(sourceContext, endpointInfos!); });
    }

    /// <summary>
    /// Analyzes a class declaration to determine if it's an endpoint and extract metadata.
    /// </summary>
    private static EndpointDefinition GetEndpointInfo(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;

        // Get the semantic model symbol for this class
        var classSymbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDecl, cancellationToken);
        if (classSymbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return null;
        }

        var mapMethodsAttributeInfo = namedTypeSymbol.GetMapMethodAttributeDefinition();

        if (mapMethodsAttributeInfo == null)
        {
            return null;
        }

        var entryPoint = namedTypeSymbol.FindEntryPointMethod(mapMethodsAttributeInfo.EntryPoint);

        return entryPoint == null
            ? null
            : EndpointDefinition.Create(namedTypeSymbol, entryPoint, mapMethodsAttributeInfo);
    }

    /// <summary>
    /// Generates the extension methods file for all discovered endpoints.
    /// </summary>
    private static void GenerateEndpointExtensions(
        SourceProductionContext context,
        ImmutableArray<EndpointDefinition> endpoints)
    {
        if (endpoints.IsEmpty)
        {
            return;
        }

        var fileScope = EndpointCodeGenerator.GenerateCode(
            "MinimalEndpoints.Generated",
            "MinimalEndpointExtensions",
            endpoints
        );

        // Add the generated source to the compilation
        context.AddSource("MinimalEndpointExtensions.g.cs", fileScope.Build());
    }
}
