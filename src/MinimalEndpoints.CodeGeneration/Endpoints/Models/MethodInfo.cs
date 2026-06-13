using System.Collections.Immutable;
using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration.Endpoints.Models;

internal sealed class MethodInfo
{
    public string Name { get; set; }

    // Parameters in source declaration order. Stored as an explicit ordered array rather than a
    // Dictionary so ordering (and the frozen equality key built from it) does not rest on
    // Dictionary's undocumented insertion-order preservation across host runtimes, and so a
    // duplicate parameter name in a mid-edit signature can never throw while building it.
    public ImmutableArray<ParameterInfo> Parameters { get; set; } = ImmutableArray<ParameterInfo>.Empty;

    public TypeDefinition ReturnType { get; set; }
}
