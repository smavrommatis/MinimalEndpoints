using System.Collections.Concurrent;
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
        // Collect all groups first to build hierarchy
        var groupsDict = new ConcurrentDictionary<INamedTypeSymbol, GroupInfo>(SymbolEqualityComparer.Default);

        // Collect groups
        context.RegisterSymbolAction(symbolContext =>
        {
            if (symbolContext.Symbol is not INamedTypeSymbol namedTypeSymbol)
            {
                return;
            }

            var mapGroupAttr = namedTypeSymbol.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "MapGroupAttribute");

            if (mapGroupAttr != null)
            {
                var prefix = mapGroupAttr.ConstructorArguments.FirstOrDefault().Value as string ?? "/";
                var parentGroupArg = mapGroupAttr.NamedArguments
                    .FirstOrDefault(arg => arg.Key == "ParentGroup");
                var parentGroup = parentGroupArg.Value.Value as INamedTypeSymbol;

                groupsDict.TryAdd(namedTypeSymbol, new GroupInfo
                {
                    Symbol = namedTypeSymbol,
                    Prefix = prefix,
                    ParentGroup = parentGroup
                });
            }
        }, SymbolKind.NamedType);

        // Collect all endpoints across the compilation
        var endpoints = new ConcurrentBag<EndpointRouteInfo>();

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

            // Get group type if specified
            var groupType = mapMethodsAttribute.GroupType;

            var pattern = mapMethodsAttribute.Pattern;

            foreach (var method in mapMethodsAttribute.Methods)
            {
                var location = namedTypeSymbol.Locations.FirstOrDefault();
                if (location == null)
                {
                    continue;
                }

                endpoints.Add(new EndpointRouteInfo
                {
                    ClassName = namedTypeSymbol.Name,
                    RoutePattern = pattern,
                    HttpMethod = method,
                    Location = location,
                    GroupType = groupType
                });
            }
        }, SymbolKind.NamedType);

        context.RegisterCompilationEndAction(compilationContext =>
        {
            // Build full paths for all endpoints including group hierarchy
            foreach (var endpoint in endpoints)
            {
                var fullPath = BuildFullPath(endpoint.RoutePattern, endpoint.GroupType, groupsDict);
                endpoint.FullPath = fullPath;
                endpoint.NormalizedPattern = NormalizeRoutePattern(fullPath);
            }

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
                            first.FullPath,
                            second.ClassName
                        );

                        compilationContext.ReportDiagnostic(diagnostic);
                    }
                }
            }
        });
    }

    private static string BuildFullPath(string pattern, INamedTypeSymbol groupType, ConcurrentDictionary<INamedTypeSymbol, GroupInfo> groups)
    {
        if (groupType == null)
        {
            return pattern;
        }

        var pathSegments = new List<string>();
        var current = groupType;

        // Walk up the group hierarchy
        while (current != null)
        {
            if (groups.TryGetValue(current, out var groupInfo))
            {
                pathSegments.Insert(0, groupInfo.Prefix.Trim('/'));
                current = groupInfo.ParentGroup;
            }
            else
            {
                break;
            }
        }

        // Add the endpoint pattern
        pathSegments.Add(pattern.Trim('/'));

        return "/" + string.Join("/", pathSegments.Where(s => !string.IsNullOrEmpty(s)));
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
        public string FullPath { get; set; }
        public string NormalizedPattern { get; set; }
        public string HttpMethod { get; set; }
        public Location Location { get; set; }
        public INamedTypeSymbol GroupType { get; set; }
    }

    private class GroupInfo
    {
        public INamedTypeSymbol Symbol { get; set; }
        public string Prefix { get; set; }
        public INamedTypeSymbol ParentGroup { get; set; }
    }
}

