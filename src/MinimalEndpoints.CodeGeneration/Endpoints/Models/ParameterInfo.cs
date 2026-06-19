using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration.Endpoints.Models;

internal sealed class ParameterInfo
{
    public string Name { get; set; }

    public TypeDefinition Type { get; set; }

    public bool Nullable { get; set; }

    // The by-reference modifier (None/Ref/Out/In/RefReadOnlyParameter). Captured so a change to a
    // parameter's modifier invalidates the cached model's equality key; without it, editing `int` to
    // `ref int` would not retrigger generation. The generator declines by-ref/pointer entry points, so
    // for an emitted endpoint this is always None — it is a determinism guard, not an emitted value.
    public RefKind RefKind { get; set; }

    public string DefaultValue { get; set; }

    public List<AttributeDefinition> Attributes { get; set; } = [];
}
