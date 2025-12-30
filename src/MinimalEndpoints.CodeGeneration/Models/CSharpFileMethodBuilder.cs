using System.Text;
using MinimalEndpoints.CodeGeneration.Utilities;

namespace MinimalEndpoints.CodeGeneration.Models;

internal sealed class CSharpFileMethodBuilder
{
    private readonly string _modifiers;
    private readonly string _returnType;
    private readonly string _methodName;
    private readonly string _parameters;
    private readonly List<(string content, int additionalIndentation)> _lines = [];

    public CSharpFileMethodBuilder(
        string modifiers,
        string returnType,
        string methodName,
        string parameters
    )
    {
        _modifiers = modifiers;
        _returnType = returnType;
        _methodName = methodName;
        _parameters = parameters;
    }

    public CSharpFileMethodBuilder AddEmptyLine() => AddLine(string.Empty);

    public CSharpFileMethodBuilder AddLine(string line, int additionalIndentation = 0)
    {
        _lines.Add((line, additionalIndentation));
        return this;
    }

    public void Build(StringBuilder sb, int indentLevel = 1)
    {
        sb.AppendLineWithIndentation($"{_modifiers} {_returnType} {_methodName}({_parameters})", indentLevel);
        sb.AppendLineWithIndentation("{", indentLevel);

        foreach (var line in _lines)
        {
            if (string.IsNullOrEmpty(line.content))
            {
                sb.AppendLine();
            }
            else
            {
                sb.AppendLineWithIndentation(line.content, line.additionalIndentation + indentLevel + 1);
            }
        }

        sb.AppendLineWithIndentation("}", indentLevel);
    }
}
