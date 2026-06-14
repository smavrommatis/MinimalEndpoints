using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;
using MinimalEndpoints.CodeGeneration.Groups.Models;

namespace MinimalEndpoints.CodeGeneration.Models;

internal class SymbolDefinitionFactory
{
    public Func<AttributeData, bool> Predicate { get; }

    private readonly Func<INamedTypeSymbol, AttributeData, AccessibilityScope, SymbolDefinition> _create;

    public SymbolDefinitionFactory(
        Func<AttributeData, bool> predicate,
        Func<INamedTypeSymbol, AttributeData, AccessibilityScope, SymbolDefinition> create
    )
    {
        Predicate = predicate;
        _create = create;
    }

    /// <summary>
    /// Creates the definition for <paramref name="symbol"/>. <paramref name="scope"/> defaults to
    /// <see cref="AccessibilityScope.SameAssembly"/>; the cross-assembly scan passes
    /// <see cref="AccessibilityScope.External"/> so the emitted surface is gated for public accessibility.
    /// </summary>
    public SymbolDefinition Create(
        INamedTypeSymbol symbol,
        AttributeData attributeData,
        AccessibilityScope scope = AccessibilityScope.SameAssembly)
        => _create(symbol, attributeData, scope);

    public static SymbolDefinition TryCreateSymbol(
        INamedTypeSymbol symbol, AccessibilityScope scope = AccessibilityScope.SameAssembly)
    {
        // Symbol-level discovery gate, shared by the generator (via ForAttributeWithMetadataName),
        // the cross-assembly scan, and the analyzers so all consumers agree by construction. Non-class
        // kinds and any rejected shape (abstract / open generic / file-local / inaccessible for the
        // given scope) are skipped — the emitter cannot legally reference them, so generating for them
        // would produce non-compiling code. The analyzers surface MINEP008 for the non-abstract
        // rejections via ClassifyShape.
        if (symbol.TypeKind != TypeKind.Class || ClassifyShape(symbol, scope) != ShapeRejection.None)
        {
            return null;
        }

        var classification = Classify(symbol);

        // Only an unambiguous single endpoint OR single group attribute yields a definition.
        // Anything else — multiple endpoint attributes, an endpoint + a group attribute, a
        // duplicated group attribute, or no recognized attribute — is left for the analyzers to
        // diagnose. Discovery must never throw here, or it crashes the whole generator (CS8785)
        // and drops every generated mapping, not just the offending class.
        try
        {
            if (classification.EndpointAttributes.Length == 1 && classification.GroupAttributes.Length == 0)
            {
                return EndpointDefinition.Factory.Create(symbol, classification.EndpointAttributes[0], scope);
            }

            if (classification.GroupAttributes.Length == 1 && classification.EndpointAttributes.Length == 0)
            {
                return EndpointGroupDefinition.Factory.Create(symbol, classification.GroupAttributes[0], scope);
            }
        }
        catch
        {
            // Defense in depth: discovery must never throw. The known mid-edit/error-state shapes
            // are guarded individually, but an unanticipated exception here would surface as
            // CS8785/AD0001 and drop generation for the ENTIRE compilation — not just this symbol.
            // Skipping the symbol degrades gracefully instead.
            return null;
        }

        return null;
    }

    /// <summary>
    /// Classifies why an endpoint/group class cannot be mapped, or <see cref="ShapeRejection.None"/>
    /// if its shape is supported. The emitter must be able to reference the class by name and
    /// instantiate it via DI; open generics, file-local types, and types whose effective
    /// accessibility is below <c>internal</c> cannot be referenced from the generated (same-assembly)
    /// code. Abstract classes are a legitimate, never-mapped base pattern (rejected here but NOT
    /// surfaced as a diagnostic). Shared by the generator gate and the analyzers' MINEP008 reporting.
    /// </summary>
    public static ShapeRejection ClassifyShape(
        INamedTypeSymbol symbol, AccessibilityScope scope = AccessibilityScope.SameAssembly)
    {
        if (symbol.IsAbstract)
        {
            return ShapeRejection.Abstract;
        }

        if (symbol.Arity > 0)
        {
            return ShapeRejection.Generic;
        }

        if (symbol.IsFileLocal)
        {
            return ShapeRejection.FileLocal;
        }

        // Same-assembly generated code may reference internal types; cross-assembly (External) host code
        // can only reference types that are public all the way up the nesting chain.
        var accessible = scope == AccessibilityScope.External
            ? IsPubliclyAccessible(symbol)
            : IsReferenceableFromGeneratedCode(symbol);
        return accessible ? ShapeRejection.None : ShapeRejection.Inaccessible;
    }

    /// <summary>
    /// A human-readable reason phrase for the MINEP008 message ("…because it is {1}.").
    /// </summary>
    public static string DescribeShapeRejection(ShapeRejection rejection) => rejection switch
    {
        ShapeRejection.Generic => "an open generic type",
        ShapeRejection.FileLocal => "a file-local type",
        ShapeRejection.Inaccessible => "less accessible than internal",
        _ => "an unsupported type"
    };

    /// <summary>
    /// True when the type (and every containing type) is at least <c>internal</c> — referenceable by
    /// SAME-assembly generated code (<c>private</c>/<c>protected</c>/<c>private protected</c> nesting is
    /// not). The cross-assembly (External) gate uses <see cref="IsPubliclyAccessible"/> instead.
    /// </summary>
    private static bool IsReferenceableFromGeneratedCode(INamedTypeSymbol symbol)
    {
        for (INamedTypeSymbol current = symbol; current != null; current = current.ContainingType)
        {
            switch (current.DeclaredAccessibility)
            {
                case Accessibility.Public:
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                    continue;
                default:
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// True when <paramref name="type"/> can be named from a DIFFERENT (host) assembly — i.e. it is
    /// <c>public</c> all the way up its nesting chain AND every generic type argument and array/pointer
    /// element is likewise publicly accessible (so <c>Task&lt;InternalDto&gt;</c> is rejected). Used by
    /// the External gate for the endpoint/group type, by MINEP009 for a referenced group, and by the
    /// endpoint factory for the emitted handler surface (return type, parameter types, ServiceType).
    /// Type parameters and intrinsic/special types are always accessible.
    /// </summary>
    public static bool IsPubliclyAccessible(ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                return IsPubliclyAccessible(array.ElementType);
            case IPointerTypeSymbol pointer:
                return IsPubliclyAccessible(pointer.PointedAtType);
            case ITypeParameterSymbol:
                return true;
            case INamedTypeSymbol named:
                for (INamedTypeSymbol current = named; current != null; current = current.ContainingType)
                {
                    if (current.DeclaredAccessibility != Accessibility.Public)
                    {
                        return false;
                    }
                }

                foreach (var argument in named.TypeArguments)
                {
                    if (!IsPubliclyAccessible(argument))
                    {
                        return false;
                    }
                }

                return true;
            default:
                return true;
        }
    }

    /// <summary>
    /// Partitions a type's attributes into endpoint-mapping attributes (MapGet, MapPost, …) and
    /// group attributes (MapGroup). Shared by <see cref="TryCreateSymbol"/> and the analyzers so
    /// the ambiguous endpoint + group case (MINEP007) can be detected explicitly rather than by
    /// catching the exception a predicate-form <c>SingleOrDefault</c> would throw.
    /// </summary>
    public static SymbolClassification Classify(INamedTypeSymbol symbol)
    {
        var endpointAttributes = ImmutableArray.CreateBuilder<AttributeData>();
        var groupAttributes = ImmutableArray.CreateBuilder<AttributeData>();

        foreach (var attributeData in symbol.GetAttributes())
        {
            if (EndpointDefinition.Factory.Predicate(attributeData))
            {
                endpointAttributes.Add(attributeData);
            }
            else if (EndpointGroupDefinition.Factory.Predicate(attributeData))
            {
                groupAttributes.Add(attributeData);
            }
        }

        return new SymbolClassification(endpointAttributes.ToImmutable(), groupAttributes.ToImmutable());
    }
}

/// <summary>
/// The endpoint- and group-mapping attributes found on a single type, as classified by
/// <see cref="SymbolDefinitionFactory.Classify"/>.
/// </summary>
internal readonly struct SymbolClassification
{
    public SymbolClassification(
        ImmutableArray<AttributeData> endpointAttributes,
        ImmutableArray<AttributeData> groupAttributes)
    {
        EndpointAttributes = endpointAttributes;
        GroupAttributes = groupAttributes;
    }

    public ImmutableArray<AttributeData> EndpointAttributes { get; }

    public ImmutableArray<AttributeData> GroupAttributes { get; }

    /// <summary>
    /// True when the type carries at least one endpoint attribute AND at least one group
    /// attribute — the invalid combination reported as MINEP007.
    /// </summary>
    public bool IsEndpointAndGroup => EndpointAttributes.Length > 0 && GroupAttributes.Length > 0;
}

/// <summary>
/// Why an attributed class cannot be mapped, as classified by
/// <see cref="SymbolDefinitionFactory.ClassifyShape"/>. <see cref="None"/> means the shape is
/// supported. <see cref="Abstract"/> is rejected silently (legitimate base pattern); the rest are
/// surfaced as MINEP008.
/// </summary>
internal enum ShapeRejection
{
    None,
    Abstract,
    Generic,
    FileLocal,
    Inaccessible
}

/// <summary>
/// Where the generated code that references a discovered type will live, which decides the minimum
/// accessibility the type must have. <see cref="SameAssembly"/> is the default FAWMN/source path
/// (generated code is in the same assembly, so <c>internal</c> is fine). <see cref="External"/> is the
/// cross-assembly scan (the host re-derives a referenced type, so it must be <c>public</c>).
/// </summary>
internal enum AccessibilityScope
{
    SameAssembly,
    External
}
