using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace MinimalEndpoints.CodeGeneration.Utilities;

internal static class StringExtensions
{
    /// <summary>
    /// Prefixes an identifier with <c>@</c> when it collides with a C# reserved keyword, so that
    /// user-chosen names like <c>@event</c> or <c>class</c> emit as valid identifiers in generated
    /// code instead of producing CS1001/CS1003. Only reserved keywords are escaped; contextual
    /// keywords (<c>var</c>, <c>async</c>, …) are valid identifiers and left untouched.
    /// </summary>
    public static string EscapeIdentifier(this string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
    }

    public static string Indent(int level = 1)
    {
        return level <= 0
            ? string.Empty
            : new string(' ', level * 4);
    }

    public static StringBuilder AppendLineWithIndentation(this StringBuilder sb, string value, int level = 1)
    {
        return sb.AppendLine(Indent(value, level));
    }

    public static string Indent(string code, int level = 1)
    {
        var indentation = Indent(level);
        var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var indentedLines = lines.Select(line => indentation + line);
        // Join with bare "\n"; CSharpFileScope.Build normalizes the whole file's newlines to "\n" at the
        // end, so this stays consistent with the StringBuilder.AppendLine sections rather than mixing EOLs.
        return string.Join("\n", indentedLines);
    }
}
