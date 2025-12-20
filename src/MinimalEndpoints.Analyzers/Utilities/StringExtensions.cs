using System.Text;

namespace MinimalEndpoints.Analyzers.Utilities;

internal static class StringExtensions
{
    public static string Indent(int level = 1)
    {
        return new string(' ', level * 4);
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
        return string.Join("\n", indentedLines);
    }
}
