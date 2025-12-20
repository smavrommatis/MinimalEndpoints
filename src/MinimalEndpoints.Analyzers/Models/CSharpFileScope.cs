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

    /// <summary>
    /// Builds the complete C# file content
    /// </summary>
    public string Build()
    {
        var sb = new StringBuilder();

        BuildHeaderSection(sb);
        BuildUsingsSection(sb);
        BuildNamespaceAndClass(sb);

        return sb.ToString();
    }

    /// <summary>
    /// Builds the header section (auto-generated comment, etc.)
    /// </summary>
    private void BuildHeaderSection(StringBuilder sb)
    {
        if (_header != null)
        {
            sb.Append(_header);
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Builds the using directives section
    /// </summary>
    private void BuildUsingsSection(StringBuilder sb)
    {
        foreach (var @using in _usings.OrderBy(u => u))
        {
            sb.AppendLine($"using {@using};");
        }

        if (_usings.Count > 0)
        {
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Builds the namespace declaration and class with methods
    /// </summary>
    private void BuildNamespaceAndClass(StringBuilder sb)
    {
        sb.AppendLine($"namespace {_namespaceName};");
        sb.AppendLine();

        BuildClassDeclaration(sb);
    }

    /// <summary>
    /// Builds the class declaration with attributes and methods
    /// </summary>
    private void BuildClassDeclaration(StringBuilder sb)
    {
        foreach (var attribute in _classAttributes)
        {
            sb.AppendLine(attribute);
        }

        sb.AppendLine($"{_modifiers} partial class {_className}");
        sb.AppendLine("{");

        BuildMethods(sb);

        sb.AppendLine("}");
    }


    /// <summary>
    /// Builds all method bodies
    /// </summary>
    private void BuildMethods(StringBuilder sb)
    {
        var methodList = _methods.Values.ToList();
        for (var i = 0; i < methodList.Count; i++)
        {
            methodList[i].Build(sb);

            // Add blank line between methods (but not after last method)
            if (i < methodList.Count - 1)
            {
                sb.AppendLine();
            }
        }
    }
}
