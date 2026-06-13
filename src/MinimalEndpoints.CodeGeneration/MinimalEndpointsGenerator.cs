using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;
using MinimalEndpoints.CodeGeneration.Groups;
using MinimalEndpoints.CodeGeneration.Groups.Models;
using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration;

[Generator]
public class MinimalEndpointsGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Tracking name of the merged-definitions provider (the input to the single source-output step).
    /// Exposed so the caching tests can assert this intermediate step is served from cache and
    /// localize a regression to it, rather than only observing the final generated text.
    /// </summary>
    internal const string MergedProviderTrackingName = "MinimalEndpointsMergedDefinitions";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register one ForAttributeWithMetadataName provider per mapping attribute. FAWMN
        // pre-indexes attribute names per compilation, so unrelated attributed classes never reach
        // the (expensive) semantic transform. Each provider's transform runs the shared classifier
        // over the MERGED symbol, so partial classes, attribute splits across parts, and ambiguous
        // multi-attribute combinations are all resolved identically regardless of which provider
        // fired (or deferred to the analyzers' MINEP002/MINEP007). Duplicate definitions across
        // providers are collapsed by FQN in GenerateEndpointExtensions.
        var merged = WellKnownTypes.Annotations.AllMapAttributeMetadataNames
            .Select(metadataName => context.SyntaxProvider.ForAttributeWithMetadataName(
                    metadataName,
                    predicate: static (node, _) => node is ClassDeclarationSyntax,
                    transform: static (ctx, _) => ctx.TargetSymbol is INamedTypeSymbol symbol
                        ? SymbolDefinitionFactory.TryCreateSymbol(symbol)
                        : null)
                .Where(static def => def is not null)
                .Collect())
            .Aggregate((accumulated, next) =>
                accumulated.Combine(next).Select(static (pair, _) => pair.Left.AddRange(pair.Right)))
            .WithTrackingName(MergedProviderTrackingName);

        context.RegisterSourceOutput(
            merged,
            static (sourceContext, definitions) => { GenerateEndpointExtensions(sourceContext, definitions!); });
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

        try
        {
            var endpoints = new List<EndpointDefinition>(definitions.Length);
            var groupDefinitions = new List<EndpointGroupDefinition>(definitions.Length);

            // Deduplicate by fully-qualified class name. ForAttributeWithMetadataName already fires
            // once per attributed declaration (so a partial class with the Map attribute on a single
            // part yields one definition), but this collapses any residual duplicates as defense in
            // depth — without it a duplicate would produce a CS0128 duplicate Handler / duplicated
            // registration in the generated file.
            var seen = new HashSet<string>();

            foreach (var def in definitions)
            {
                switch (def)
                {
                    case EndpointDefinition endpoint when seen.Add(endpoint.ClassType.FullName):
                        endpoints.Add(endpoint);
                        break;
                    case EndpointGroupDefinition group when seen.Add(group.ClassType.FullName):
                        groupDefinitions.Add(group);
                        break;
                }
            }

            // Compute the hierarchy in a transient, FQN-keyed structure rather than mutating the
            // cached models — this resolves the endpoint→group and parent→group links by name (stable
            // across incremental compilations) instead of by a symbol from a stale compilation.
            var hierarchy = GroupHierarchy.Build(groupDefinitions);

            var fileScope = MinimalEndpointsFileBuilder.GenerateFile(
                "MinimalEndpoints.Generated",
                "MinimalEndpointExtensions",
                endpoints,
                hierarchy
            );

            if (fileScope is null)
            {
                return;
            }

            // Add the generated source to the compilation
            context.AddSource("MinimalEndpointExtensions.g.cs", fileScope.Build());
        }
        catch (Exception ex)
        {
            // Surface an unexpected generator failure as a clear, actionable build error (MINEP999)
            // instead of the opaque CS8785. Reporting an Error diagnostic fails the build with a
            // readable message — it does not swallow the failure, it makes it visible.
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.GeneratorFailure,
                Location.None,
                $"{ex.GetType().Name}: {ex.Message}"));
        }
    }
}
