using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.CodeGeneration.Tests.Groups.Analyzers;

public class GroupsAnalyzer_CyclicGroupHierarchyTests
{
    [Fact]
    public void WithDirectCycle_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"", ParentGroup = typeof(ApiGroup))]
public class ApiGroup { }";
        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var error = Assert.Single(diagnostics, d => d.Id == "MINEP006");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("ApiGroup", error.GetMessage());
    }

    [Fact]
    public void WithTwoLevelCycle_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"", ParentGroup = typeof(V1Group))]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }";
        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var errors = diagnostics.Where(d => d.Id == "MINEP006").ToList();
        Assert.NotEmpty(errors);
        Assert.All(errors, e => Assert.Equal(DiagnosticSeverity.Error, e.Severity));
    }

    [Fact]
    public void WithThreeLevelCycle_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"", ParentGroup = typeof(V2Group))]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }

[MapGroup(""/v2"", ParentGroup = typeof(V1Group))]
public class V2Group { }";
        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var errors = diagnostics.Where(d => d.Id == "MINEP006").ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void WithValidHierarchy_NoError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }

[MapGroup(""/products"", ParentGroup = typeof(V1Group))]
public class ProductsGroup { }";
        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP006");
    }

    [Fact]
    public void FourLevelCycle_DetectsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"", ParentGroup = typeof(V3Group))]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }

[MapGroup(""/v2"", ParentGroup = typeof(V1Group))]
public class V2Group { }

[MapGroup(""/v3"", ParentGroup = typeof(V2Group))]
public class V3Group { }";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var errors = diagnostics.Where(d => d.Id == "MINEP006").ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void MultipleSeparateCycles_DetectsAll()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api1"", ParentGroup = typeof(V1Group))]
public class Api1Group { }

[MapGroup(""/v1"", ParentGroup = typeof(Api1Group))]
public class V1Group { }

[MapGroup(""/api2"", ParentGroup = typeof(V2Group))]
public class Api2Group { }

[MapGroup(""/v2"", ParentGroup = typeof(Api2Group))]
public class V2Group { }";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var errors = diagnostics.Where(d => d.Id == "MINEP006").ToList();
        Assert.True(errors.Count >= 2, "Should detect multiple cycles");
    }

    [Fact]
    public void DiamondShape_NoErrorIfNoCycle()
    {
        // Arrange - Diamond pattern without cycle
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1GroupA { }

[MapGroup(""/v2"", ParentGroup = typeof(ApiGroup))]
public class V1GroupB { }

[MapGroup(""/admin"", ParentGroup = typeof(V1GroupA))]
public class AdminGroup { }";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP006");
    }

    [Fact]
    public void FiveLevelCycle_DetectsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/g1"", ParentGroup = typeof(Group5))]
public class Group1 { }

[MapGroup(""/g2"", ParentGroup = typeof(Group1))]
public class Group2 { }

[MapGroup(""/g3"", ParentGroup = typeof(Group2))]
public class Group3 { }

[MapGroup(""/g4"", ParentGroup = typeof(Group3))]
public class Group4 { }

[MapGroup(""/g5"", ParentGroup = typeof(Group4))]
public class Group5 { }";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var errors = diagnostics.Where(d => d.Id == "MINEP006").ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void WithValidAndCyclicGroups_DetectsOnlyCycles()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/valid"")]
public class ValidGroup { }

[MapGroup(""/child"", ParentGroup = typeof(ValidGroup))]
public class ValidChildGroup { }

[MapGroup(""/cyclic1"", ParentGroup = typeof(CyclicGroup2))]
public class CyclicGroup1 { }

[MapGroup(""/cyclic2"", ParentGroup = typeof(CyclicGroup1))]
public class CyclicGroup2 { }";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var errors = diagnostics.Where(d => d.Id == "MINEP006").ToList();
        Assert.NotEmpty(errors);
        // Valid groups should not be reported
        Assert.DoesNotContain(errors, e => e.GetMessage().Contains("ValidGroup") || e.GetMessage().Contains("ValidChildGroup"));
    }

    [Fact]
    public void ComplexDiamond_NoError()
    {
        // Arrange - More complex diamond (multiple paths to same node)
        var code = @"
namespace TestApp;

[MapGroup(""/root"")]
public class RootGroup { }

[MapGroup(""/branch1"", ParentGroup = typeof(RootGroup))]
public class Branch1Group { }

[MapGroup(""/branch2"", ParentGroup = typeof(RootGroup))]
public class Branch2Group { }

[MapGroup(""/branch3"", ParentGroup = typeof(RootGroup))]
public class Branch3Group { }

[MapGroup(""/leaf1"", ParentGroup = typeof(Branch1Group))]
public class Leaf1Group { }

[MapGroup(""/leaf2"", ParentGroup = typeof(Branch2Group))]
public class Leaf2Group { }";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert - No cycles, just complex branching
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP006");
    }

    private List<Diagnostic> GetDiagnostics(string code)
    {
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build(validateCompilation: false);

        return CompilationUtilities.GenerateDiagnostics(compilation);
    }

}

