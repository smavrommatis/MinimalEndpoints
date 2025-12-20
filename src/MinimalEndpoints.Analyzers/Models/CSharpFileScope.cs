using System.Text;

namespace MinimalEndpoints.Analyzers.Models;

internal sealed class CSharpFileScope
{
    private readonly string _modifiers;
    private readonly string _namespaceName;
    private readonly string _className;
    private readonly HashSet<string> _usings = [];
    private readonly Dictionary<string, CSharpFileMethodBuilder> _methods = [];
    private readonly List<string> _classAttributes = [];
    private string _header;


    /// <summary>
    /// Gets the available usings for simplifying type names
    /// </summary>
    public HashSet<string> GetAvailableUsings() => _usings;

    public CSharpFileScope(string modifiers, string namespaceName, string className)
    {
        _modifiers = modifiers;
        _namespaceName = namespaceName;
        _className = className;
    }

    public CSharpFileScope WithHeader(string header)
    {
        _header = header;
        return this;
    }

    public CSharpFileScope AddUsing(string @namespace)
    {
        _usings.Add(@namespace.Trim().TrimEnd(';'));
        return this;
    }

    public CSharpFileScope AddClassAttribute(string attribute)
    {
        _classAttributes.Add(attribute);
        return this;
    }

    public CSharpFileMethodBuilder AddMethod(string modifiers, string returnType, string methodName, string parameters)
    {
        if (!_methods.TryGetValue(methodName, out var method))
        {
            method = new CSharpFileMethodBuilder(modifiers, returnType, methodName, parameters);
            _methods[methodName] = method;
        }

        return method;
    }

    public string Build()
    {
        var sb = new StringBuilder(_header ?? string.Empty);

        if (_header != null)
        {
            sb.AppendLine();
        }

        foreach (var @using in _usings)
        {
            sb.AppendLine($"using {@using};");
        }

        sb.AppendLine();
        sb.AppendLine($"namespace {_namespaceName};");
        sb.AppendLine();
        foreach (var attribute in _classAttributes)
        {
            sb.AppendLine(attribute);
        }

        sb.AppendLine($"{_modifiers} partial class {_className}");
        sb.AppendLine("{");

        foreach (var method in _methods.Values)
        {
            method.Build(sb);
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }
}
