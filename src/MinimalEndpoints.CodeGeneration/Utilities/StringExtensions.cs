using System.Text;

namespace MinimalEndpoints.CodeGeneration.Utilities;

internal static class StringExtensions
{
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
        return string.Join("\n", indentedLines);
    }
}
