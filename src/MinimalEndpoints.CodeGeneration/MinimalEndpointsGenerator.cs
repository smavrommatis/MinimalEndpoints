using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;
using MinimalEndpoints.CodeGeneration.Groups;
using MinimalEndpoints.CodeGeneration.Groups.Models;
using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration;

[Generator]
public class MinimalEndpointsGenerator : IIncrementalGenerator
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
                transform: TryGetDefinition)
            .Where(static def => def is not null); // Filter out nulls

        // Collect all endpoint infos and generate code when compilation changes
        context.RegisterSourceOutput(
            endpointClasses.Collect(),
            static (sourceContext, definitions) => { GenerateEndpointExtensions(sourceContext, definitions!); });
    }

    /// <summary>
    /// Analyzes a class declaration to determine if it's an endpoint and extract metadata.
    /// </summary>
    private static SymbolDefinition TryGetDefinition(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;

        // Get the semantic model symbol for this class
        var classSymbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDecl, cancellationToken);
        return classSymbol is not INamedTypeSymbol symbol
            ? null
            : SymbolDefinitionFactory.TryCreateSymbol(symbol);
    }

    /// <summary>
    /// Generates the extension methods file for all discovered endpoints.
    /// </summary>
    private static void GenerateEndpointExtensions(
        SourceProductionContext context,
        ImmutableArray<SymbolDefinition> definitions)
    {
        if (definitions.IsEmpty)
        {
            return;
        }

        var endpoints = new List<EndpointDefinition>(definitions.Length);
        var groupDefinitions = new List<EndpointGroupDefinition>(definitions.Length);
        Dictionary<INamedTypeSymbol, EndpointGroupDefinition> groups;

        foreach (var def in definitions)
        {
            switch (def)
            {
                case EndpointDefinition endpoint:
                    endpoints.Add(endpoint);
                    break;
                case EndpointGroupDefinition group:
                    groupDefinitions.Add(group);
                    break;
            }
        }

        groups = groupDefinitions.Count > 0
            ? groupDefinitions.FillHierarchyAndDetectCycles()
            : new Dictionary<INamedTypeSymbol, EndpointGroupDefinition>(SymbolEqualityComparer.Default);

        var fileScope = MinimalEndpointsFileBuilder.GenerateFile(
            "MinimalEndpoints.Generated",
            "MinimalEndpointExtensions",
            endpoints,
            groups
        );

        // Add the generated source to the compilation
        context.AddSource("MinimalEndpointExtensions.g.cs", fileScope.Build());
    }
}
