using System.Collections.Generic;
using System.Linq;
using MinimalEndpoints.CodeGeneration.Groups.Models;

namespace MinimalEndpoints.CodeGeneration.Groups;

/// <summary>
/// A transient, per-execution view of the group hierarchy, built fresh from the immutable
/// <see cref="EndpointGroupDefinition"/>s each time output is produced (in the generator) or
/// diagnostics are computed (in the analyzer). It keys everything by fully-qualified name — never
/// a Roslyn symbol — so the parent/child links resolve correctly across incremental compilations,
/// and it carries all the depth/prefix/cycle state that used to be memoized (and mutated) on the
/// cached models. Nothing here is stored back on the definitions, so the cached pipeline objects
/// stay immutable.
/// </summary>
internal sealed class GroupHierarchy
{
    public readonly struct CycleInfo
    {
        public CycleInfo(EndpointGroupDefinition group, IReadOnlyList<string> names)
        {
            Group = group;
            Names = names;
        }

        /// <summary>
        /// A group that actually participates in the cycle (the member with the smallest
        /// fully-qualified name), chosen deterministically so the diagnostic location is stable
        /// across builds rather than flapping with iteration order.
        /// </summary>
        public EndpointGroupDefinition Group { get; }

        /// <summary>The simple names along the actual cycle path, e.g. <c>[A, B, A]</c>.</summary>
        public IReadOnlyList<string> Names { get; }
    }

    private readonly List<EndpointGroupDefinition> _ordered;
    private readonly Dictionary<string, EndpointGroupDefinition> _byName;

    // Resolved parent FQN per group FQN; null once the parent is absent or the edge was broken to
    // break a cycle. Every group FQN is a key here.
    private readonly Dictionary<string, string> _parent = new();
    private readonly Dictionary<string, int> _depth = new();
    private readonly Dictionary<string, string> _fullPrefix = new();
    private readonly Dictionary<string, bool> _hierarchyConditional = new();
    private readonly List<CycleInfo> _cycles = new();

    private GroupHierarchy(List<EndpointGroupDefinition> ordered, Dictionary<string, EndpointGroupDefinition> byName)
    {
        _ordered = ordered;
        _byName = byName;
    }

    public static GroupHierarchy Build(IEnumerable<EndpointGroupDefinition> groups)
    {
        var ordered = new List<EndpointGroupDefinition>();
        var byName = new Dictionary<string, EndpointGroupDefinition>();

        foreach (var group in groups)
        {
            // Last-writer-wins on duplicate FQN (defense in depth — discovery already de-dups).
            if (byName.ContainsKey(group.ClassType.FullName))
            {
                byName[group.ClassType.FullName] = group;
                continue;
            }

            byName[group.ClassType.FullName] = group;
            ordered.Add(group);
        }

        var hierarchy = new GroupHierarchy(ordered, byName);
        hierarchy.ResolveParents();
        hierarchy.DetectAndBreakCycles();
        hierarchy.ComputeDerived();
        return hierarchy;
    }

    private void ResolveParents()
    {
        foreach (var group in _ordered)
        {
            var parentName = group.ParentGroupName;
            _parent[group.ClassType.FullName] =
                parentName != null && _byName.ContainsKey(parentName) ? parentName : null;
        }
    }

    private void DetectAndBreakCycles()
    {
        // Walk groups in a deterministic FQN-sorted order: the analyzer feeds groups from a
        // ConcurrentDictionary whose enumeration order is unspecified, which previously made cycle
        // attribution and which-edge-gets-broken flap between builds.
        foreach (var group in _ordered.OrderBy(g => g.ClassType.FullName, StringComparer.Ordinal))
        {
            var visited = new HashSet<string>();
            var path = new List<string>();
            string prev = null;
            var current = group.ClassType.FullName;

            while (current != null)
            {
                path.Add(current);

                if (visited.Add(current))
                {
                    prev = current;
                    current = _parent[current];
                }
                else
                {
                    // Cycle detected. Break the edge that closes it so the rest of the build sees an
                    // acyclic hierarchy and the same cycle is not re-reported from another start node.
                    if (prev != null)
                    {
                        _parent[prev] = null;
                    }

                    // Trim the lead-in: the actual cycle runs from the first occurrence of the
                    // repeated node to the end (e.g. D -> A -> B -> C -> A records only A -> B -> C -> A).
                    var cycleStart = path.IndexOf(current);
                    var cycleFqns = path.GetRange(cycleStart, path.Count - cycleStart);

                    // Attribute the diagnostic to a node genuinely in the cycle — the smallest FQN —
                    // rather than the (possibly lead-in) start node, for stable, correct blame.
                    var ownerFqn = cycleFqns
                        .Take(cycleFqns.Count - 1)
                        .OrderBy(fqn => fqn, StringComparer.Ordinal)
                        .First();

                    _cycles.Add(new CycleInfo(
                        _byName[ownerFqn],
                        cycleFqns.Select(fqn => _byName[fqn].Name).ToArray()));
                    break;
                }
            }
        }
    }

    private void ComputeDerived()
    {
        foreach (var group in _ordered)
        {
            ComputeDepth(group.ClassType.FullName);
            ComputeFullPrefix(group.ClassType.FullName);
            ComputeHierarchyConditional(group.ClassType.FullName);
        }
    }

    private int ComputeDepth(string fqn)
    {
        if (_depth.TryGetValue(fqn, out var depth))
        {
            return depth;
        }

        var parent = _parent[fqn];
        depth = parent == null ? 1 : ComputeDepth(parent) + 1;
        _depth[fqn] = depth;
        return depth;
    }

    private string ComputeFullPrefix(string fqn)
    {
        if (_fullPrefix.TryGetValue(fqn, out var prefix))
        {
            return prefix;
        }

        var parent = _parent[fqn];
        var ownPrefix = _byName[fqn].Prefix;
        prefix = parent == null ? ownPrefix : JoinWithSingleSlash(ComputeFullPrefix(parent), ownPrefix);
        _fullPrefix[fqn] = prefix;
        return prefix;
    }

    /// <summary>
    /// Joins a route prefix and a following segment (a parent group's full prefix to a child's own
    /// prefix, or a group prefix to an endpoint pattern) with exactly one separating slash, matching
    /// how ASP.NET combines nested <c>MapGroup</c> prefixes at runtime. A direct concatenation turned a
    /// right side lacking a leading slash ("v1") plus its left ("/api") into "/apiv1", desyncing the
    /// analyzer's computed full route from the real "/api/v1" route and hiding genuine MINEP004
    /// conflicts. (Any residual doubled slash is collapsed downstream by the route normalizer, so only
    /// the MISSING-slash case needs handling here.) Shared by <see cref="GroupHierarchy"/> and the
    /// route-overlap analyzer so the two never diverge in how they join routes.
    /// </summary>
    internal static string JoinWithSingleSlash(string left, string right)
    {
        var leftTrimmed = (left ?? string.Empty).TrimEnd('/');
        var rightValue = right ?? string.Empty;

        if (rightValue.Length == 0)
        {
            return leftTrimmed;
        }

        return rightValue[0] == '/' ? leftTrimmed + rightValue : leftTrimmed + "/" + rightValue;
    }

    private bool ComputeHierarchyConditional(string fqn)
    {
        if (_hierarchyConditional.TryGetValue(fqn, out var conditional))
        {
            return conditional;
        }

        var parent = _parent[fqn];
        conditional = _byName[fqn].IsConditionallyMapped ||
                      (parent != null && ComputeHierarchyConditional(parent));
        _hierarchyConditional[fqn] = conditional;
        return conditional;
    }

    public int Count => _byName.Count;

    /// <summary>The groups in discovery order.</summary>
    public IReadOnlyList<EndpointGroupDefinition> Groups => _ordered;

    public IReadOnlyList<CycleInfo> Cycles => _cycles;

    public bool TryGet(string fullyQualifiedName, out EndpointGroupDefinition group) =>
        _byName.TryGetValue(fullyQualifiedName, out group);

    public EndpointGroupDefinition Parent(EndpointGroupDefinition group)
    {
        var parent = _parent[group.ClassType.FullName];
        return parent == null ? null : _byName[parent];
    }

    public int DepthOf(EndpointGroupDefinition group) => _depth[group.ClassType.FullName];

    public string FullPrefixOf(EndpointGroupDefinition group) => _fullPrefix[group.ClassType.FullName];

    public bool HierarchyConditionallyMappedOf(EndpointGroupDefinition group) =>
        _hierarchyConditional[group.ClassType.FullName];
}
