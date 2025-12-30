using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;
using MinimalEndpoints.CodeGeneration.Groups.Models;
using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration.Groups.Analyzers;

/// <summary>
/// Analyzer that detects ambiguous route patterns across endpoints.
/// Reports MINEP004 when multiple endpoints have the same route pattern and HTTP method.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GroupsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            Diagnostics.AmbiguousRoutes,
            Diagnostics.CyclicGroupHierarchy,
            Diagnostics.InvalidSymbolKind
        ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }

    private static void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        // Collect all endpoints across the compilation
        var definitions = new ConcurrentDictionary<INamedTypeSymbol, SymbolDefinition>(SymbolEqualityComparer.Default);

        context.RegisterSymbolAction(symbolContext =>
        {
            if (symbolContext.Symbol is not INamedTypeSymbol namedTypeSymbol)
            {
                return;
            }

            try
            {
                var definition = SymbolDefinitionFactory.TryCreateSymbol(namedTypeSymbol);

                if (definition != null)
                {
                    definitions.TryAdd(namedTypeSymbol, definition);
                }
            }
            catch (InvalidOperationException ex)
            {
                var invalidSymbolDiagnostic = Diagnostic.Create(
                    Diagnostics.InvalidSymbolKind,
                    namedTypeSymbol.Locations.FirstOrDefault(),
                    namedTypeSymbol.Name
                );
                symbolContext.ReportDiagnostic(invalidSymbolDiagnostic);
            }

        }, SymbolKind.NamedType);

        context.RegisterCompilationEndAction(compilationContext =>
        {
            if (definitions.IsEmpty)
            {
                return;
            }

            var endpoints = new List<EndpointDefinition>(definitions.Count);
            var groupDefinitions = new List<EndpointGroupDefinition>(definitions.Count);
            Dictionary<INamedTypeSymbol, EndpointGroupDefinition> groups;

            foreach (var def in definitions.Values)
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

            ReportAmbiguousRoutes(compilationContext, endpoints, groups);
            ReportCyclicGroupHierarchies(compilationContext, [..groups.Values]);
        });
    }

    private static void ReportCyclicGroupHierarchies(
        CompilationAnalysisContext compilationContext,
        ImmutableArray<EndpointGroupDefinition> groups
    )
    {
        foreach (var group in groups)
        {
            if (group.Cycles.Count == 0)
            {
                continue;
            }

            foreach (var cycle in group.Cycles)
            {
                var cycleString = string.Join(" -> ", cycle.Select(g => g.Symbol.Name));
                var diagnostic = Diagnostic.Create(
                    Diagnostics.CyclicGroupHierarchy,
                    group.Symbol.Locations.FirstOrDefault(),
                    group.Symbol.Name,
                    cycleString
                );

                compilationContext.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static void ReportAmbiguousRoutes(CompilationAnalysisContext compilationContext,
        List<EndpointDefinition> endpoints, Dictionary<INamedTypeSymbol, EndpointGroupDefinition> groups)
    {
        var endpointsByPath = endpoints
            .Select(x =>
            {
                var path = BuildFullPath(x, groups);

                return new
                {
                    Endpoint = x,
                    Path = path,
                    NormalizedPattern = NormalizeRoutePattern(path),
                    HttpMethods = x.MapMethodsAttribute.Methods,
                };
            })
            .SelectMany(x => x.HttpMethods.Select(httpMethod => new
            {
                HttpMethod = httpMethod, x.NormalizedPattern, x.Path, Endpoint = x.Endpoint,
            }))
            .GroupBy(x => (x.NormalizedPattern, x.HttpMethod))
            .Where(x => x.Count() > 1)
            .ToList();

        // Report diagnostics for ambiguous routes
        foreach (var group in endpointsByPath)
        {
            var endpointList = group.ToList();

            // Report for all but skip reporting the same pair multiple times
            for (var i = 0; i < endpointList.Count; i++)
            {
                for (var j = i + 1; j < endpointList.Count; j++)
                {
                    var first = endpointList[i];
                    var second = endpointList[j];


                    var diagnostic = Diagnostic.Create(
                        Diagnostics.AmbiguousRoutes,
                        first.Endpoint.Symbol.Locations.FirstOrDefault(),
                        first.Endpoint.Symbol.Name,
                        first.Path,
                        second.Endpoint.Symbol.Name
                    );

                    compilationContext.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static string BuildFullPath(EndpointDefinition endpointDefinition,
        Dictionary<INamedTypeSymbol, EndpointGroupDefinition> groups)
    {
        if (
            endpointDefinition.MapMethodsAttribute.GroupType == null
            || !groups.TryGetValue(endpointDefinition.MapMethodsAttribute.GroupType, out var groupDefinition)
        )
        {
            return endpointDefinition.MapMethodsAttribute.Pattern ?? string.Empty;
        }

        return groupDefinition.FullPrefix + endpointDefinition.MapMethodsAttribute.Pattern;
    }

    private static string NormalizeRoutePattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return string.Empty;
        }

        // Trim leading and trailing slashes
        var normalized = pattern.Trim('/');

        // Convert to lowercase for case-insensitive comparison
        normalized = normalized.ToLowerInvariant();

        // Replace ALL route parameters with a generic placeholder
        // This correctly identifies ambiguous routes:
        // - /{id:int} and /{userId:int} both become /{param}
        // - /{id} and /{name} both become /{param}
        // - /users/{id}/posts/{postId} becomes /users/{param}/posts/{param}
        normalized = Regex.Replace(
            normalized,
            @"\{[^}]+\}", // Match any {parameter}, {parameter:constraint}, or {**catchall}
            "{param}" // Replace with generic placeholder
        );

        return normalized;
    }
}
