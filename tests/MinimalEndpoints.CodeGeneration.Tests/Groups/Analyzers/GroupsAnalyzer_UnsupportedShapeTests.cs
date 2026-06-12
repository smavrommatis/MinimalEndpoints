using Microsoft.CodeAnalysis;
using static MinimalEndpoints.Tests.Common.CompilationUtilities;

namespace MinimalEndpoints.CodeGeneration.Tests.Groups.Analyzers;

public class GroupsAnalyzer_UnsupportedShapeTests
{
    [Fact]
    public void GenericGroup_ReportsMinep008()
    {
        // An open generic group cannot be referenced/instantiated from the generated code, so the
        // GroupsAnalyzer reports MINEP008 (the EndpointsAnalyzer never inspects group classes).
        var code = @"
namespace TestApp;

[MapGroup(""/g"")]
public class Group<T>
{
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP008");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("Group", warning.GetMessage());
    }

    [Fact]
    public void ValidGroup_NoMinep008()
    {
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup
{
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP008");
    }

    [Fact]
    public void AbstractGroup_NoMinep008()
    {
        // Abstract groups are skipped silently (legitimate base pattern) — NOT reported.
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public abstract class BaseGroup
{
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP008");
    }
}
