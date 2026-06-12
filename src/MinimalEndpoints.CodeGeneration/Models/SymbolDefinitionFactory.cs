using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;
using MinimalEndpoints.CodeGeneration.Groups.Models;

namespace MinimalEndpoints.CodeGeneration.Models;

internal class SymbolDefinitionFactory
{
    public Func<AttributeData, bool> Predicate { get; }
    public Func<INamedTypeSymbol, AttributeData, SymbolDefinition> Create { get; }

    public SymbolDefinitionFactory(
        Func<AttributeData, bool> predicate,
        Func<INamedTypeSymbol, AttributeData, SymbolDefinition> create
    )
    {
        Predicate = predicate;
        Create = create;
    }

    public static SymbolDefinition TryCreateSymbol(INamedTypeSymbol symbol)
    {
        // Symbol-level discovery gate, shared by the generator (via ForAttributeWithMetadataName)
        // and the analyzers so all consumers agree by construction. Non-class kinds and any
        // rejected shape (abstract / open generic / file-local / inaccessible) are skipped — the
        // emitter cannot legally reference them, so generating for them would produce non-compiling
        // code. The analyzers surface MINEP008 for the non-abstract rejections via ClassifyShape.
        if (symbol.TypeKind != TypeKind.Class || ClassifyShape(symbol) != ShapeRejection.None)
        {
            return null;
        }

        var classification = Classify(symbol);

        // Only an unambiguous single endpoint OR single group attribute yields a definition.
        // Anything else — multiple endpoint attributes, an endpoint + a group attribute, a
        // duplicated group attribute, or no recognized attribute — is left for the analyzers to
        // diagnose. Discovery must never throw here, or it crashes the whole generator (CS8785)
        // and drops every generated mapping, not just the offending class.
        if (classification.EndpointAttributes.Length == 1 && classification.GroupAttributes.Length == 0)
        {
            return EndpointDefinition.Factory.Create(symbol, classification.EndpointAttributes[0]);
        }

        if (classification.GroupAttributes.Length == 1 && classification.EndpointAttributes.Length == 0)
        {
            return EndpointGroupDefinition.Factory.Create(symbol, classification.GroupAttributes[0]);
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
    public static ShapeRejection ClassifyShape(INamedTypeSymbol symbol)
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

        return IsReferenceableFromGeneratedCode(symbol) ? ShapeRejection.None : ShapeRejection.Inaccessible;
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
    /// True when the type (and every containing type) is at least <c>internal</c>. Generated code
    /// lives in the same assembly, so <c>internal</c> and <c>protected internal</c> are referenceable;
    /// <c>private</c>, <c>protected</c>, and <c>private protected</c> nesting are not.
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
