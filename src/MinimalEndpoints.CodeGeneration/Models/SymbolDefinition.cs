using System;
using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.CodeGeneration.Models;

internal abstract class SymbolDefinition : IEquatable<SymbolDefinition>
{
    protected SymbolDefinition(INamedTypeSymbol symbol)
    {
        Symbol = symbol;
    }

    public INamedTypeSymbol Symbol { get; }

    /// <summary>
    /// A stable, value-based key over everything that affects this definition's generated
    /// output, captured from transform-time data only (never from mutable hierarchy state).
    /// Two definitions of the same kind are equal iff their keys match.
    /// </summary>
    /// <remarks>
    /// This is what lets the incremental generator's <c>Collect()</c> output be served from
    /// cache. The raw <see cref="Symbol"/> is recreated per compilation, so relying on it for
    /// equality would force reference equality and miss the cache on every edit — even edits to
    /// unrelated files. Because <c>RegisterSourceOutput</c> caches over the whole collected
    /// array, capturing each definition's own inputs is sufficient: any relevant change flips
    /// that definition's key and breaks array equality, triggering regeneration.
    /// </remarks>
    protected abstract string EqualityKey { get; }

    public bool Equals(SymbolDefinition other) =>
        other is not null && GetType() == other.GetType() && EqualityKey == other.EqualityKey;

    public override bool Equals(object obj) => Equals(obj as SymbolDefinition);

    public override int GetHashCode() => EqualityKey.GetHashCode();
}
