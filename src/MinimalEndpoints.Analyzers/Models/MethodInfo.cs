namespace MinimalEndpoints.Analyzers.Models;

internal sealed class MethodInfo
{
    public string Name { get; set; }

    public List<ParameterInfo> Parameters { get; set; } = [];

    public string ReturnType { get; set; }

    public bool IsAsync { get; set; }
}
