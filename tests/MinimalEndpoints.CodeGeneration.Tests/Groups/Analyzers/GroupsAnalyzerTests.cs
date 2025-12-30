using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.CodeGeneration.Tests.Groups.Analyzers;

/// <summary>
/// Tests for the MinimalEndpointsAnalyzer diagnostic analyzer.
/// </summary>
public class GroupsAnalyzerTests
{
    #region MINEP004 - Ambiguous Routes Tests

    [Fact]
    public void AmbiguousRoutes_WithDuplicateRoutes_ReportsWarning()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/users"")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}

[MapGet(""/users"")]
public class ListUsersEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("GetUsersEndpoint", warning.GetMessage());
        Assert.Contains("ListUsersEndpoint", warning.GetMessage());
    }

    [Fact]
    public void AmbiguousRoutes_WithSamePatternDifferentMethods_NoWarning()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/users"")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}

[MapPost(""/users"")]
public class CreateUserEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP004");
    }

    [Fact]
    public void AmbiguousRoutes_WithDifferentPatterns_NoWarning()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/users"")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}

[MapGet(""/users/active"")]
public class GetActiveUsersEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP004");
    }

    [Fact]
    public void AmbiguousRoutes_WithCaseInsensitiveMatch_ReportsWarning()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/api/users"")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}

[MapGet(""/API/USERS"")]
public class GetUsersUpperEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void AmbiguousRoutes_WithRouteConstraints_StillReports()
    {
        // Arrange - Different constraints but same pattern structure
        var code = @"
namespace TestApp;

[MapGet(""/items/{id:int}"")]
public class GetItemByIdEndpoint
{
    public Task<IResult> HandleAsync(int id)
    {
        return Task.FromResult(Results.Ok());
    }
}

[MapGet(""/items/{userId:int}"")]
public class GetItemByUserIdEndpoint
{
    public Task<IResult> HandleAsync(int userId)
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        // Should report warning as both patterns are {id:int} after normalization
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void AmbiguousRoutes_WithThreeConflicts_ReportsMultiple()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/data"")]
public class GetData1Endpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/data"")]
public class GetData2Endpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/data"")]
public class GetData3Endpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        // With 3 endpoints, we should get 3 warnings (each pair)
        var warnings = diagnostics.Where(d => d.Id == "MINEP004").ToList();
        Assert.Equal(3, warnings.Count);
    }

    [Fact]
    public void AmbiguousRoutes_WithGroupHierarchy_DetectsConflicts()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }

[MapGet(""/products"", Group = typeof(V1Group))]
public class GetProductsV1Endpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/api/v1/products"")]
public class GetProductsV1DirectEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert - Both resolve to /api/v1/products
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void AmbiguousRoutes_WithDifferentGroupHierarchies_NoConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }

[MapGroup(""/v2"", ParentGroup = typeof(ApiGroup))]
public class V2Group { }

[MapGet(""/products"", Group = typeof(V1Group))]
public class GetProductsV1Endpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/products"", Group = typeof(V2Group))]
public class GetProductsV2Endpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert - /api/v1/products vs /api/v2/products - no conflict
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP004");
    }

    #endregion

    #region MINEP006 - Cyclic Group Hierarchy Tests

    [Fact]
    public void CyclicGroupHierarchy_WithDirectCycle_ReportsError()
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
    public void CyclicGroupHierarchy_WithTwoLevelCycle_ReportsError()
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
    public void CyclicGroupHierarchy_WithThreeLevelCycle_ReportsError()
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
    public void CyclicGroupHierarchy_WithValidHierarchy_NoError()
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

    #endregion

    #region MINEP007 - Invalid Symbol Kind Tests

    [Fact]
    public void InvalidSymbolKind_WithEndpointAndGroupAttributes_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/users"")]
[MapGroup(""/api"")]
public class TestEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}
";
        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.Contains(diagnostics, d => d.Id == "MINEP007");
    }

    #endregion

    #region MINEP004 - Additional Edge Cases

    [Fact]
    public void AmbiguousRoutes_WithTrailingSlash_DetectsConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/api/users"")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/api/users/"")]
public class GetUsersWithSlashEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void AmbiguousRoutes_WithOptionalParameters_DetectsConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/users/{id?}"")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync(int? id) => Task.FromResult(Results.Ok());
}

[MapGet(""/users/{userId?}"")]
public class GetUsersByIdEndpoint
{
    public Task<IResult> HandleAsync(int? userId) => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void AmbiguousRoutes_WithCatchAll_DetectsConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/{**path}"")]
public class CatchAllEndpoint1
{
    public Task<IResult> HandleAsync(string path) => Task.FromResult(Results.Ok());
}

[MapGet(""/{**route}"")]
public class CatchAllEndpoint2
{
    public Task<IResult> HandleAsync(string route) => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void AmbiguousRoutes_DifferentConstraintsSamePattern_StillReports()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/items/{id:int:min(1)}"")]
public class GetItemEndpoint
{
    public Task<IResult> HandleAsync(int id) => Task.FromResult(Results.Ok());
}

[MapGet(""/items/{id:int:max(1000)}"")]
public class GetItemWithMaxEndpoint
{
    public Task<IResult> HandleAsync(int id) => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    #endregion

    #region MINEP006 - Additional Edge Cases

    [Fact]
    public void CyclicGroupHierarchy_FourLevelCycle_DetectsError()
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
    public void CyclicGroupHierarchy_MultipleSeparateCycles_DetectsAll()
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
    public void CyclicGroupHierarchy_DiamondShape_NoErrorIfNoCycle()
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

    #endregion

    private List<Diagnostic> GetDiagnostics(string code)
    {
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build(validateCompilation: false);

        return CompilationUtilities.GenerateDiagnostics(compilation);
    }

}

