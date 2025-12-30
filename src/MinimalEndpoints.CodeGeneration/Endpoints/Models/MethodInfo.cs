using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration.Endpoints.Models;

internal sealed class MethodInfo
{
    public string Name { get; set; }

    public Dictionary<string, ParameterInfo> Parameters { get; set; } = [];

    public TypeDefinition ReturnType { get; set; }

    public bool IsAsync { get; set; }
}
