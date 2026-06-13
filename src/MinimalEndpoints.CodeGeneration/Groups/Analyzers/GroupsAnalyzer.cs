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
        ImmutableArray.Create(
            Diagnostics.AmbiguousRoutes,
            Diagnostics.CyclicGroupHierarchy,
            Diagnostics.InvalidSymbolKind,
            Diagnostics.UnsupportedEndpointShape);

    // Compiled once and reused rather than re-parsed on every Regex.Replace call. Collapses runs of
    // interior slashes ("a//b" -> "a/b").
    private static readonly Regex DuplicateSlashRegex = new("/{2,}", RegexOptions.Compiled);

    // Replaces a route parameter token — {parameter}, {parameter:constraint}, {**catchall}, with
    // {{ }} brace escapes — with a generic placeholder so routes that differ only by parameter
    // name/constraint compare equal.
    private static readonly Regex RouteParameterRegex = new(@"\{(?:[^{}]|\{\{|\}\})*\}", RegexOptions.Compiled);

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
            .SelectMany(x =>
            {
                var path = BuildFullPath(x, hierarchy);

                // An endpoint can occupy more than one normalized pattern (a trailing optional
                // parameter also matches the bare path), and is matched once per HTTP verb.
                return EffectiveNormalizedPatterns(path)
                    .SelectMany(pattern => NormalizeVerbs(x.Methods).Select(httpMethod => new
                    {
                        HttpMethod = httpMethod, NormalizedPattern = pattern, Path = path, x.Symbol,
                    }));
            })
            .GroupBy(x => (x.NormalizedPattern, x.HttpMethod))
            .Where(x => x.Count() > 1)
            .ToList();

        // A given pair of endpoints can only collide on one normalized pattern (each endpoint has a
        // single pattern), so two endpoints overlapping on several verbs would otherwise be reported
        // once per shared verb — visually identical duplicate warnings. Collapse to one diagnostic
        // per unordered endpoint pair.
        var reportedPairs = new HashSet<(string, string)>();

        foreach (var group in endpointsByPath)
        {
            var endpointList = group.ToList();

            for (var i = 0; i < endpointList.Count; i++)
            {
                for (var j = i + 1; j < endpointList.Count; j++)
                {
                    var first = endpointList[i];
                    var second = endpointList[j];

                    // An endpoint can land in the same (pattern, verb) bucket more than once — a
                    // duplicated verb in one [MapMethods] array, or its own optional/bare-path
                    // expansion. Never pair an endpoint with itself ("X conflicts with X").
                    if (SymbolEqualityComparer.Default.Equals(first.Symbol, second.Symbol))
                    {
                        continue;
                    }

                    var firstName = first.Symbol.ToDisplayString();
                    var secondName = second.Symbol.ToDisplayString();
                    var pairKey = string.CompareOrdinal(firstName, secondName) <= 0
                        ? (firstName, secondName)
                        : (secondName, firstName);

                    if (!reportedPairs.Add(pairKey))
                    {
                        continue;
                    }

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

    /// <summary>
    /// Normalizes an endpoint's HTTP verbs for conflict comparison. Verb casing is upper-cased to
    /// match the generator (so <c>[MapMethods("/a", new[]{"get"})]</c> and <c>[MapGet("/a")]</c>
    /// compare equal). An endpoint with no resolved verbs (an empty or all-null methods array) still
    /// occupies its path, so it is represented by a single unspecified-verb sentinel rather than
    /// being dropped from analysis — letting two such endpoints on one path still surface a conflict.
    /// </summary>
    private static IEnumerable<string> NormalizeVerbs(string[] methods)
    {
        if (methods == null || methods.Length == 0)
        {
            return new[] { string.Empty };
        }

        return methods.Select(method => (method ?? string.Empty).ToUpperInvariant());
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

    /// <summary>
    /// The normalized route pattern(s) an endpoint occupies for conflict comparison. Usually one,
    /// but a trailing optional parameter ("/users/{id?}" or a defaulted "/users/{id=5}") also
    /// matches the path with that segment absent, so the endpoint additionally occupies the bare
    /// path and can collide with a separate endpoint mapped there.
    /// </summary>
    private static IEnumerable<string> EffectiveNormalizedPatterns(string path)
    {
        yield return NormalizeRoutePattern(path);

        if (TryStripTrailingOptionalSegment(path, out var barePath))
        {
            yield return NormalizeRoutePattern(barePath);
        }
    }

    /// <summary>
    /// When <paramref name="path"/> ends with an OPTIONAL route parameter — "{name?}" or a defaulted
    /// "{name=value}" — yields the path with that final segment removed (the route the endpoint also
    /// matches). A required "{name}" or a literal segment is not omittable and yields nothing.
    /// </summary>
    private static bool TryStripTrailingOptionalSegment(string path, out string barePath)
    {
        barePath = null;
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var trimmed = path.TrimEnd('/');
        var lastSlash = trimmed.LastIndexOf('/');
        var lastSegment = lastSlash >= 0 ? trimmed.Substring(lastSlash + 1) : trimmed;

        var isOptional = lastSegment.Length > 2 &&
                         lastSegment[0] == '{' &&
                         lastSegment[lastSegment.Length - 1] == '}' &&
                         (lastSegment[lastSegment.Length - 2] == '?' || lastSegment.Contains("="));
        if (!isOptional)
        {
            return false;
        }

        barePath = lastSlash >= 0 ? trimmed.Substring(0, lastSlash) : string.Empty;
        return true;
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
        normalized = DuplicateSlashRegex.Replace(normalized, "/");

        // Replace ALL route parameters with a generic placeholder. This correctly identifies
        // ambiguous routes:
        // - /{id:int} and /{userId:int} both become /{param}
        // - /{id} and /{name} both become /{param}
        // - /users/{id}/posts/{postId} becomes /users/{param}/posts/{param}
        //
        // The token body allows the doubled braces ASP.NET uses to escape a literal brace or a
        // regex constraint's own quantifier braces (e.g. "{id:regex(^\d{{3}}$)}"). The previous
        // "\{[^}]+\}" stopped at the first '}', so a doubled '}}' leaked and left "{param}}$)}",
        // hiding a real conflict with a plain "{id}" route.
        normalized = RouteParameterRegex.Replace(normalized, "{param}");

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
