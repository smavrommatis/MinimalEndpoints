using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MinimalEndpoints.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GroupHierarchyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Diagnostics.CyclicGroupHierarchy];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Use CompilationStartAction to collect groups and analyze at compilation end
        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            var groupTypes = new List<INamedTypeSymbol>();
            var lockObject = new object();

            // Register symbol action to collect all group types
            compilationStartContext.RegisterSymbolAction(symbolContext =>
            {
                if (symbolContext.Symbol is not INamedTypeSymbol namedType)
                    return;

                var hasMapGroupAttr = namedType.GetAttributes()
                    .Any(attr => attr.AttributeClass?.Name == "MapGroupAttribute");

                if (hasMapGroupAttr)
                {
                    lock (lockObject)
                    {
                        groupTypes.Add(namedType);
                    }
                }
            }, SymbolKind.NamedType);

            // Check for cycles at compilation end
            compilationStartContext.RegisterCompilationEndAction(compilationEndContext =>
            {
                foreach (var groupType in groupTypes)
                {
                    var cycle = DetectCycle(groupType);
                    if (cycle != null)
                    {
                        var cycleString = string.Join(" -> ", cycle.Select(g => g.Name));
                        var diagnostic = Diagnostic.Create(
                            Diagnostics.CyclicGroupHierarchy,
                            groupType.Locations.FirstOrDefault(),
                            groupType.Name,
                            cycleString
                        );

                        compilationEndContext.ReportDiagnostic(diagnostic);
                    }
                }
            });
        });
    }

    private static List<INamedTypeSymbol> DetectCycle(INamedTypeSymbol startGroup)
    {
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var path = new List<INamedTypeSymbol>();

        return DetectCycleRecursive(startGroup, visited, path);
    }

    private static List<INamedTypeSymbol> DetectCycleRecursive(
        INamedTypeSymbol currentGroup,
        HashSet<INamedTypeSymbol> visited,
        List<INamedTypeSymbol> path)
    {
        // If we've already visited this group in the current path, we have a cycle
        if (path.Contains(currentGroup, SymbolEqualityComparer.Default))
        {
            // Return the cycle path
            var cycleStart = path.IndexOf(currentGroup);
            var cycle = path.Skip(cycleStart).ToList();
            cycle.Add(currentGroup); // Close the cycle
            return cycle;
        }

        // If we've already fully explored this group, no cycle from here
        if (visited.Contains(currentGroup))
        {
            return null;
        }

        path.Add(currentGroup);

        // Get parent group
        var parentType = GetParentGroupType(currentGroup);
        if (parentType != null)
        {
            var result = DetectCycleRecursive(parentType, visited, path);
            if (result != null)
            {
                return result;
            }
        }

        path.Remove(currentGroup);
        visited.Add(currentGroup);

        return null;
    }

    private static INamedTypeSymbol GetParentGroupType(INamedTypeSymbol groupType)
    {
        var mapGroupAttr = groupType.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "MapGroupAttribute");

        var parentGroupArg = mapGroupAttr?.NamedArguments
            .FirstOrDefault(arg => arg.Key == "ParentGroup");

        return parentGroupArg?.Value.Value as INamedTypeSymbol;
    }
}

