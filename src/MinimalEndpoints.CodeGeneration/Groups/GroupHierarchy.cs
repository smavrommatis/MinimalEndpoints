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

        /// <summary>The group at which the cycle was recorded (the walk's starting group).</summary>
        public EndpointGroupDefinition Group { get; }

        /// <summary>The simple names along the detected cycle path, e.g. <c>[A, B, A]</c>.</summary>
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
        foreach (var group in _ordered)
        {
            var visited = new HashSet<string>();
            var path = new List<string>(capacity: 1);
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
                    // Cycle detected: break the last edge and record the path (matching the
                    // previous symbol-based DetectAndBreakCycles behaviour exactly).
                    if (prev != null)
                    {
                        _parent[prev] = null;
                    }

                    _cycles.Add(new CycleInfo(group, path.Select(fqn => _byName[fqn].Name).ToArray()));
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
        prefix = (parent == null ? "" : ComputeFullPrefix(parent)) + _byName[fqn].Prefix;
        _fullPrefix[fqn] = prefix;
        return prefix;
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
