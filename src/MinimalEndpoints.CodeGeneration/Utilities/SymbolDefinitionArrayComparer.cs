using System.Collections.Generic;
using System.Collections.Immutable;
using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration.Utilities;

/// <summary>
/// Structural, element-wise equality for an <see cref="ImmutableArray{T}"/> of
/// <see cref="SymbolDefinition"/>, comparing each element by its value equality
/// (<see cref="SymbolDefinition"/>'s EqualityKey).
/// <para>
/// This is load-bearing on the cross-assembly scan node. That node is fed by
/// <c>CompilationProvider</c>, which yields a brand-new <c>Compilation</c> — and therefore a
/// freshly-allocated result array — on every keystroke. The default <see cref="ImmutableArray{T}"/>
/// equality is backing-array <em>reference</em> equality, so without this comparer every edit would
/// look "changed" and cascade into a full regeneration. With it, an equal-content scan result
/// compares equal to the cached one and the (unchanged) output is served from cache.
/// </para>
/// </summary>
internal sealed class SymbolDefinitionArrayComparer : IEqualityComparer<ImmutableArray<SymbolDefinition>>
{
    public static readonly SymbolDefinitionArrayComparer Instance = new();

    private SymbolDefinitionArrayComparer()
    {
    }

    public bool Equals(ImmutableArray<SymbolDefinition> x, ImmutableArray<SymbolDefinition> y)
    {
        // Treat default and empty as equal (both have no elements), matching GetHashCode which collapses
        // both to 0 — so a future Empty/default swap on an early-out can't spuriously look "changed".
        if (x.IsDefaultOrEmpty || y.IsDefaultOrEmpty)
        {
            return x.IsDefaultOrEmpty && y.IsDefaultOrEmpty;
        }

        // SequenceEqual uses EqualityComparer<SymbolDefinition>.Default, which dispatches to
        // SymbolDefinition's IEquatable implementation (value equality over the EqualityKey).
        return x.SequenceEqual(y);
    }

    public int GetHashCode(ImmutableArray<SymbolDefinition> obj)
    {
        if (obj.IsDefaultOrEmpty)
        {
            return 0;
        }

        var hash = 17;
        foreach (var definition in obj)
        {
            hash = (hash * 31) + (definition?.GetHashCode() ?? 0);
        }

        return hash;
    }
}
