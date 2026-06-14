using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration;

/// <summary>
/// The host's cross-assembly scan opt-in, parsed once from the compilation's assembly attributes.
/// Shared by the generator (<see cref="ReferencedAssemblyScanner"/>) and the analyzers (MINEP009) so
/// both agree on exactly which referenced assemblies are scanned. <see cref="Resolve"/> returns
/// <see langword="null"/> when the host has not opted in.
/// </summary>
internal sealed class ScanReferencedEndpointsOptIn
{
    // Always non-null; empty => scan ALL referenced assemblies that use MinimalEndpoints.
    private readonly HashSet<IAssemblySymbol> _targets;

    private ScanReferencedEndpointsOptIn(HashSet<IAssemblySymbol> targets) => _targets = targets;

    /// <summary>
    /// True when <paramref name="assembly"/> is in scope for scanning: any assembly when no explicit
    /// targets were given (empty set), otherwise only the assemblies named by the marker's type args.
    /// </summary>
    public bool Covers(IAssemblySymbol assembly) => _targets.Count == 0 || _targets.Contains(assembly);

    /// <summary>
    /// Parses <c>[assembly: ScanReferencedEndpoints(...)]</c> off the compilation's own assembly.
    /// Returns <see langword="null"/> when the attribute is absent (the host has not opted in).
    /// </summary>
    public static ScanReferencedEndpointsOptIn Resolve(Compilation compilation)
    {
        var attributeType = compilation.GetTypeByMetadataName(
            WellKnownTypes.Annotations.ScanReferencedEndpointsAttributeFullName);
        if (attributeType is null)
        {
            return null;
        }

        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
            {
                return new ScanReferencedEndpointsOptIn(ResolveTargets(attribute));
            }
        }

        return null;
    }

    /// <summary>
    /// True when <paramref name="groupType"/> is a <c>public</c> group in a referenced assembly the host
    /// will NOT scan (no opt-in, or a targeted opt-in that excludes it) — the condition under which
    /// referencing it silently drops the group's prefix/configuration. Drives MINEP009.
    /// <para>
    /// Non-public referenced groups are deliberately excluded: the host cannot even name one (the
    /// <c>typeof(...)</c> is itself CS0122), so the compiler already reports the problem and a MINEP009 on
    /// top would be redundant. <paramref name="optIn"/> is resolved once per compilation by the caller
    /// (and may be <see langword="null"/> when the host has not opted in at all).
    /// </para>
    /// </summary>
    public static bool IsReferencedButNotScanned(
        Compilation compilation, ScanReferencedEndpointsOptIn optIn, INamedTypeSymbol groupType)
    {
        var groupAssembly = groupType.ContainingAssembly;
        if (groupAssembly is null ||
            SymbolEqualityComparer.Default.Equals(groupAssembly, compilation.Assembly) ||
            !SymbolDefinitionFactory.IsPubliclyAccessible(groupType))
        {
            return false;
        }

        return optIn is null || !optIn.Covers(groupAssembly);
    }

    private static HashSet<IAssemblySymbol> ResolveTargets(AttributeData attribute)
    {
        var targets = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        // The single `params Type[]` constructor argument surfaces as one array-kind TypedConstant
        // whose elements are the typeof(...) marker types; each contributes its containing assembly.
        foreach (var argument in attribute.ConstructorArguments)
        {
            if (argument.Kind != TypedConstantKind.Array)
            {
                continue;
            }

            foreach (var element in argument.Values)
            {
                if (element.Value is INamedTypeSymbol type && type.ContainingAssembly is { } assembly)
                {
                    targets.Add(assembly);
                }
            }
        }

        return targets;
    }
}
