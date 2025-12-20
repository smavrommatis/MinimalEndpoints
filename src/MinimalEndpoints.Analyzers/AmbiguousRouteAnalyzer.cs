using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MinimalEndpoints.Analyzers.Utilities;

namespace MinimalEndpoints.Analyzers;

/// <summary>
/// Analyzer that detects ambiguous route patterns across endpoints.
/// Reports MINEP004 when multiple endpoints have the same route pattern and HTTP method.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AmbiguousRouteAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Diagnostics.AmbiguousRoutes];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }

    private static void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        // Collect all endpoints across the compilation
        var endpoints = new List<EndpointRouteInfo>();
        var syncLock = new object();

        context.RegisterSymbolAction(symbolContext =>
        {
            if (symbolContext.Symbol is not INamedTypeSymbol namedTypeSymbol)
            {
                return;
            }

            var attributes = namedTypeSymbol.GetMapMethodAttributes();
            if (attributes.Length == 0)
            {
                return;
            }

            var mapMethodsAttribute = attributes[0].GetMapMethodAttributeDefinition();
            if (mapMethodsAttribute == null)
            {
                return;
            }

            // Normalize the pattern (remove leading/trailing slashes for comparison)
            var normalizedPattern = NormalizeRoutePattern(mapMethodsAttribute.Pattern);

            foreach (var method in mapMethodsAttribute.Methods)
            {
                var location = namedTypeSymbol.Locations.FirstOrDefault();
                if (location == null)
                {
                    continue;
                }

                lock (syncLock)
                {
                    endpoints.Add(new EndpointRouteInfo
                    {
                        ClassName = namedTypeSymbol.Name,
                        RoutePattern = mapMethodsAttribute.Pattern,
                        NormalizedPattern = normalizedPattern,
                        HttpMethod = method,
                        Location = location
                    });
                }
            }
        }, SymbolKind.NamedType);

        context.RegisterCompilationEndAction(compilationContext =>
        {
            // Group by normalized pattern and HTTP method to find duplicates
            var grouped = endpoints
                .GroupBy(e => (e.NormalizedPattern, e.HttpMethod))
                .Where(g => g.Count() > 1)
                .ToList();

            // Report diagnostics for ambiguous routes
            foreach (var group in grouped)
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
                            first.Location,
                            first.ClassName,
                            first.RoutePattern,
                            second.ClassName
                        );

                        compilationContext.ReportDiagnostic(diagnostic);
                    }
                }
            }
        });
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
        normalized = System.Text.RegularExpressions.Regex.Replace(
            normalized,
            @"\{[^}]+\}",  // Match any {parameter}, {parameter:constraint}, or {**catchall}
            "{param}"       // Replace with generic placeholder
        );

        return normalized;
    }

    private class EndpointRouteInfo
    {
        public string ClassName { get; set; }
        public string RoutePattern { get; set; }
        public string NormalizedPattern { get; set; }
        public string HttpMethod { get; set; }
        public Location Location { get; set; }
    }
}

