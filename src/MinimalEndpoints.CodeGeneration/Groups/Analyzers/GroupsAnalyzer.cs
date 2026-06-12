using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MinimalEndpoints.CodeGeneration.Endpoints;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;
using MinimalEndpoints.CodeGeneration.Groups.Models;
using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration.Groups.Analyzers;

/// <summary>
/// Analyzer that detects ambiguous route patterns across endpoints (MINEP004), cyclic group
/// hierarchies (MINEP006), endpoint+group classes (MINEP007), and unsupported group shapes
/// (MINEP008). It runs over a single compilation, so it works with live symbols.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GroupsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            Diagnostics.AmbiguousRoutes,
            Diagnostics.CyclicGroupHierarchy,
            Diagnostics.InvalidSymbolKind,
            Diagnostics.UnsupportedEndpointShape
        ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }

    private static void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        // Only the lightweight facts each diagnostic actually needs are collected — not full
        // EndpointDefinitions (TypeDefinitions for every parameter/return, formatted attribute
        // strings) which MINEP004/MINEP006 never read. Groups stay as EndpointGroupDefinition,
        // which is already lightweight (name/prefix/parent strings only).
        var endpointFacts = new ConcurrentDictionary<INamedTypeSymbol, EndpointRouteFacts>(SymbolEqualityComparer.Default);
        var groups = new ConcurrentDictionary<INamedTypeSymbol, EndpointGroupDefinition>(SymbolEqualityComparer.Default);

        context.RegisterSymbolAction(symbolContext =>
        {
            if (symbolContext.Symbol is not INamedTypeSymbol namedTypeSymbol)
            {
                return;
            }

            // Cheap, non-allocating pre-check: bail out before the allocating Classify() for the
            // overwhelming majority of types that carry no Map/Group attribute at all.
            if (!HasMapOrGroupAttribute(namedTypeSymbol))
            {
                return;
            }

            var classification = SymbolDefinitionFactory.Classify(namedTypeSymbol);

            if (classification.IsEndpointAndGroup)
            {
                // A class decorated with both an endpoint attribute and [MapGroup] is invalid.
                var invalidSymbolDiagnostic = Diagnostic.Create(
                    Diagnostics.InvalidSymbolKind,
                    namedTypeSymbol.Locations.FirstOrDefault(),
                    namedTypeSymbol.Name
                );
                symbolContext.ReportDiagnostic(invalidSymbolDiagnostic);
                return;
            }

            // Beyond this point only classes can be endpoints/groups (structs/interfaces/enums are
            // never generated). MINEP007 above is preserved for any kind that carries both attributes.
            if (namedTypeSymbol.TypeKind != TypeKind.Class)
            {
                return;
            }

            var shape = SymbolDefinitionFactory.ClassifyShape(namedTypeSymbol);

            if (classification.EndpointAttributes.Length == 1 && classification.GroupAttributes.Length == 0)
            {
                // Endpoint class. Unsupported non-abstract shapes are reported as MINEP008 by the
                // EndpointsAnalyzer; skip them here to avoid a duplicate diagnostic.
                if (shape != ShapeRejection.None)
                {
                    return;
                }

                var facts = TryGetEndpointRouteFacts(namedTypeSymbol, classification.EndpointAttributes[0]);
                if (facts.HasValue)
                {
                    endpointFacts.TryAdd(namedTypeSymbol, facts.Value);
                }

                return;
            }

            if (classification.GroupAttributes.Length == 1 && classification.EndpointAttributes.Length == 0)
            {
                // Group class. The EndpointsAnalyzer never looks at group classes, so MINEP008 for
                // an unsupported (non-abstract) group shape is reported here. Abstract is skipped
                // silently (legitimate base pattern).
                if (shape == ShapeRejection.Abstract)
                {
                    return;
                }

                if (shape != ShapeRejection.None)
                {
                    var unsupportedShapeDiagnostic = Diagnostic.Create(
                        Diagnostics.UnsupportedEndpointShape,
                        namedTypeSymbol.Locations.FirstOrDefault(),
                        namedTypeSymbol.Name,
                        SymbolDefinitionFactory.DescribeShapeRejection(shape)
                    );
                    symbolContext.ReportDiagnostic(unsupportedShapeDiagnostic);
                    return;
                }

                var group = new EndpointGroupDefinition(namedTypeSymbol, classification.GroupAttributes[0]);
                groups.TryAdd(namedTypeSymbol, group);
            }

            // Anything else (e.g. multiple endpoint attributes) is left to the EndpointsAnalyzer.
        }, SymbolKind.NamedType);

        context.RegisterCompilationEndAction(compilationContext =>
        {
            if (endpointFacts.IsEmpty && groups.IsEmpty)
            {
                return;
            }

            var groupDefinitions = new List<EndpointGroupDefinition>(groups.Count);
            var groupSymbolsByName = new Dictionary<string, INamedTypeSymbol>();

            foreach (var pair in groups)
            {
                groupDefinitions.Add(pair.Value);
                groupSymbolsByName[pair.Value.ClassType.FullName] = pair.Key;
            }

            var hierarchy = GroupHierarchy.Build(groupDefinitions);

            ReportAmbiguousRoutes(compilationContext, endpointFacts.Values, hierarchy);
            ReportCyclicGroupHierarchies(compilationContext, hierarchy, groupSymbolsByName);
        });
    }

    private static bool HasMapOrGroupAttribute(INamedTypeSymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (EndpointDefinition.Factory.Predicate(attribute) || EndpointGroupDefinition.Factory.Predicate(attribute))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Extracts the route facts MINEP004 needs without building a full endpoint model. Returns
    /// <c>null</c> for a malformed attribute or an endpoint with no valid entry point — preserving
    /// the previous behaviour, where such endpoints were excluded from ambiguity analysis because
    /// the full-model factory returned null.
    /// </summary>
    private static EndpointRouteFacts? TryGetEndpointRouteFacts(INamedTypeSymbol symbol, AttributeData endpointAttribute)
    {
        var mapDefinition = endpointAttribute.GetMapMethodAttributeDefinition();
        if (mapDefinition == null)
        {
            return null;
        }

        if (symbol.FindEntryPointMethod(mapDefinition.EntryPoint) == null)
        {
            return null;
        }

        return new EndpointRouteFacts(symbol, mapDefinition.Pattern, mapDefinition.Methods, mapDefinition.GroupTypeName);
    }

    private static void ReportCyclicGroupHierarchies(
        CompilationAnalysisContext compilationContext,
        GroupHierarchy hierarchy,
        Dictionary<string, INamedTypeSymbol> groupSymbolsByName
    )
    {
        foreach (var cycle in hierarchy.Cycles)
        {
            var cycleString = string.Join(" -> ", cycle.Names);
            groupSymbolsByName.TryGetValue(cycle.Group.ClassType.FullName, out var symbol);

            var diagnostic = Diagnostic.Create(
                Diagnostics.CyclicGroupHierarchy,
                symbol?.Locations.FirstOrDefault(),
                cycle.Group.Name,
                cycleString
            );

            compilationContext.ReportDiagnostic(diagnostic);
        }
    }

    private static void ReportAmbiguousRoutes(CompilationAnalysisContext compilationContext,
        IEnumerable<EndpointRouteFacts> endpoints, GroupHierarchy hierarchy)
    {
        var endpointsByPath = endpoints
            .Select(x =>
            {
                var path = BuildFullPath(x, hierarchy);

                return new
                {
                    x.Symbol,
                    Path = path,
                    NormalizedPattern = NormalizeRoutePattern(path),
                    HttpMethods = x.Methods,
                };
            })
            .SelectMany(x => x.HttpMethods.Select(httpMethod => new
            {
                // Normalize verb casing to match the generator (which uppercases each verb), so e.g.
                // [MapMethods("/a", new[] { "get" })] and [MapGet("/a")] are recognized as the same
                // method and their route conflict is detected.
                HttpMethod = (httpMethod ?? string.Empty).ToUpperInvariant(), x.NormalizedPattern, x.Path, x.Symbol,
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
                        first.Symbol.Locations.FirstOrDefault(),
                        first.Symbol.Name,
                        first.Path,
                        second.Symbol.Name
                    );

                    compilationContext.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static string BuildFullPath(EndpointRouteFacts facts, GroupHierarchy hierarchy)
    {
        if (
            facts.GroupTypeName == null
            || !hierarchy.TryGet(facts.GroupTypeName, out var groupDefinition)
        )
        {
            return facts.Pattern ?? string.Empty;
        }

        return JoinRoute(hierarchy.FullPrefixOf(groupDefinition), facts.Pattern);
    }

    /// <summary>
    /// Joins a group prefix and an endpoint pattern with exactly one separating slash, so a trailing
    /// slash on the prefix (or a leading slash on the pattern) does not produce a phantom "//"
    /// segment that would hide a real conflict (e.g. "/api/" + "/users" must equal "/api/users").
    /// </summary>
    private static string JoinRoute(string prefix, string pattern)
    {
        var left = (prefix ?? string.Empty).TrimEnd('/');
        var right = pattern ?? string.Empty;

        if (right.Length == 0)
        {
            return left;
        }

        return right[0] == '/' ? left + right : left + "/" + right;
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

        // Collapse interior duplicate slashes ("a//b" -> "a/b") so a stray slash in a pattern or at
        // a prefix/pattern boundary does not mask an otherwise-identical route.
        normalized = Regex.Replace(normalized, "/{2,}", "/");

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

/// <summary>
/// The minimal route information MINEP004 needs from an endpoint — extracted directly from the Map
/// attribute without building a full endpoint model. The <see cref="Symbol"/> is the live symbol
/// from the current compilation (used only for the diagnostic location and name).
/// </summary>
internal readonly struct EndpointRouteFacts
{
    public EndpointRouteFacts(INamedTypeSymbol symbol, string pattern, string[] methods, string groupTypeName)
    {
        Symbol = symbol;
        Pattern = pattern;
        Methods = methods;
        GroupTypeName = groupTypeName;
    }

    public INamedTypeSymbol Symbol { get; }

    public string Pattern { get; }

    public string[] Methods { get; }

    public string GroupTypeName { get; }
}
