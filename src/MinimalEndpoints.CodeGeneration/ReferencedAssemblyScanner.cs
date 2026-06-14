using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration;

/// <summary>
/// Discovers endpoint/group definitions in the host's <em>referenced compiled assemblies</em> — the
/// metadata that <c>ForAttributeWithMetadataName</c> (syntax-bound) cannot see. Gated by the host
/// opting in with <c>[assembly: ScanReferencedEndpoints]</c>; returns <see cref="ImmutableArray{T}.Empty"/>
/// at zero cost otherwise. It runs the SAME <see cref="SymbolDefinitionFactory"/> classifier as source
/// discovery — only in <see cref="AccessibilityScope.External"/> (public-only) scope — and returns a
/// deterministically-ordered, value-equatable array so the incremental pipeline can cache it.
/// </summary>
internal static class ReferencedAssemblyScanner
{
    public static ImmutableArray<SymbolDefinition> Scan(Compilation compilation, CancellationToken cancellationToken)
    {
        // Opt-in gate — checked BEFORE touching any references, so the default (no-attribute) path is
        // zero-cost and the generated output stays byte-identical to before. `optIn` also carries the
        // optional target-assembly set (empty => scan all referencing assemblies).
        var optIn = ScanReferencedEndpointsOptIn.Resolve(compilation);
        if (optIn is null)
        {
            return ImmutableArray<SymbolDefinition>.Empty;
        }

        // The Map attributes live in the MinimalEndpoints assembly. A referenced assembly can only
        // carry [Map*]/[MapGroup] types if it references that assembly — the cheap, exact prune that
        // drops the BCL / ASP.NET / unrelated packages before any (expensive) type enumeration.
        var annotationsAssembly = compilation
            .GetTypeByMetadataName(WellKnownTypes.Annotations.MapGroupAttributeFullName)
            ?.ContainingAssembly;
        if (annotationsAssembly is null)
        {
            return ImmutableArray<SymbolDefinition>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<SymbolDefinition>();

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip the MinimalEndpoints assembly itself, any assembly outside the opt-in's target set,
            // and any assembly that does not reference MinimalEndpoints (so it cannot contain
            // Map*-attributed types — this prunes the BCL/ASP.NET before any type enumeration).
            if (SymbolEqualityComparer.Default.Equals(assembly, annotationsAssembly) ||
                !optIn.Covers(assembly) ||
                !ReferencesAssembly(assembly, annotationsAssembly))
            {
                continue;
            }

            foreach (var type in EnumerateNamedTypes(assembly.GlobalNamespace, cancellationToken))
            {
                var definition = SymbolDefinitionFactory.TryCreateSymbol(type, AccessibilityScope.External);
                if (definition is not null)
                {
                    builder.Add(definition);
                }
            }
        }

        // Deterministic emission order regardless of ReferencedAssemblySymbols / enumeration order
        // (Roslyn guarantees neither across builds). Kept local to this node so the source-only output
        // is unaffected. Source definitions are concatenated AFTER these by the caller, so the merged
        // order is [source…, referenced-sorted-by-FQN…].
        builder.Sort(static (left, right) =>
            string.CompareOrdinal(left.FullName, right.FullName));

        return builder.ToImmutable();
    }

    private static bool ReferencesAssembly(IAssemblySymbol assembly, IAssemblySymbol target)
    {
        foreach (var module in assembly.Modules)
        {
            foreach (var referenced in module.ReferencedAssemblySymbols)
            {
                if (SymbolEqualityComparer.Default.Equals(referenced, target))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNamedTypes(
        INamespaceSymbol @namespace, CancellationToken cancellationToken)
    {
        foreach (var type in @namespace.GetTypeMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var nested in EnumerateWithNested(type))
            {
                yield return nested;
            }
        }

        foreach (var childNamespace in @namespace.GetNamespaceMembers())
        {
            foreach (var type in EnumerateNamedTypes(childNamespace, cancellationToken))
            {
                yield return type;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateWithNested(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var nested in type.GetTypeMembers())
        {
            foreach (var deeper in EnumerateWithNested(nested))
            {
                yield return deeper;
            }
        }
    }
}
