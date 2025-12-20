using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MinimalEndpoints.Analyzers.Tests;

/// <summary>
/// Tests for the MinimalEndpointsAnalyzer diagnostic analyzer.
/// </summary>
public class MinimalEndpointsAnalyzerTests
{
    #region MINEP001 - Missing Entry Point Tests

    [Fact]
    public void MissingEntryPoint_WithNoHandleMethod_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    // No Handle or HandleAsync method
    public void SomeOtherMethod() { }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var error = Assert.Single(diagnostics, d => d.Id == "MINEP001");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("TestEndpoint", error.GetMessage());
    }

    [Fact]
    public void MissingEntryPoint_WithCustomEntryPointNotFound_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"", EntryPoint = ""CustomMethod"")]
public class TestEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var error = Assert.Single(diagnostics, d => d.Id == "MINEP001");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
    }

    [Fact]
    public void MissingEntryPoint_WithCorrectMethod_NoError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP001");
    }

    [Fact]
    public void MissingEntryPoint_WithHandleMethod_NoError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public IResult Handle()
    {
        return Results.Ok();
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP001");
    }

    [Fact]
    public void MissingEntryPoint_WithCustomEntryPoint_NoError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"", EntryPoint = ""Execute"")]
public class TestEndpoint
{
    public IResult Execute()
    {
        return Results.Ok();
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP001");
    }

    #endregion

    #region MINEP002 - Multiple Attributes Tests

    [Fact]
    public void MultipleAttributes_WithTwoMapAttributes_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
[MapPost(""/test"")]
public class TestEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var error = Assert.Single(diagnostics, d => d.Id == "MINEP002");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("TestEndpoint", error.GetMessage());
    }

    [Fact]
    public void MultipleAttributes_WithSingleAttribute_NoError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP002");
    }

    #endregion

    #region MINEP003 - ServiceType Validation Tests

    [Fact]
    public void ServiceTypeValidation_WithMatchingInterface_NoError()
    {
        // Arrange
        var code = @"
namespace TestApp;

public interface ITestEndpoint
{
    Task<IResult> HandleAsync();
}

[MapGet(""/test"", ServiceType = typeof(ITestEndpoint))]
public class TestEndpoint : ITestEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP003");
    }

    [Fact]
    public void ServiceTypeValidation_WithNonMatchingInterface_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

public interface ITestEndpoint
{
    // Interface doesn't have HandleAsync
    void SomeOtherMethod();
}

[MapGet(""/test"", ServiceType = typeof(ITestEndpoint))]
public class TestEndpoint : ITestEndpoint
{
    public void SomeOtherMethod() { }

    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var error = Assert.Single(diagnostics, d => d.Id == "MINEP003");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("ITestEndpoint", error.GetMessage());
        Assert.Contains("TestEndpoint", error.GetMessage());
        Assert.Contains("HandleAsync", error.GetMessage());
    }

    [Fact]
    public void ServiceTypeValidation_WithCustomEntryPoint_ValidatesCorrectMethod()
    {
        // Arrange
        var code = @"
namespace TestApp;

public interface ITestEndpoint
{
    Task<IResult> Execute();
}

[MapGet(""/test"", ServiceType = typeof(ITestEndpoint), EntryPoint = ""Execute"")]
public class TestEndpoint : ITestEndpoint
{
    public Task<IResult> Execute()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP003");
    }

    [Fact]
    public void ServiceTypeValidation_WithCustomEntryPointMissing_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

public interface ITestEndpoint
{
    Task<IResult> HandleAsync();
}

[MapGet(""/test"", ServiceType = typeof(ITestEndpoint), EntryPoint = ""Execute"")]
public class TestEndpoint : ITestEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }

    public Task<IResult> Execute()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var error = Assert.Single(diagnostics, d => d.Id == "MINEP003");
        Assert.Contains("Execute", error.GetMessage());
    }

    #endregion

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
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

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
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGroup(""/v2"", ParentGroup = typeof(ApiGroup))]
public class V2Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

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

    #region MINEP005 - Invalid Group Type Tests

    [Fact]
    public void InvalidGroupType_WithValidGroup_NoError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api/v1"")]
public class ApiV1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGet(""/products"", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP005");
    }

    [Fact]
    public void InvalidGroupType_WithoutMapGroupAttribute_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

// Missing [MapGroup] attribute
public class ApiV1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGet(""/products"", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var error = Assert.Single(diagnostics, d => d.Id == "MINEP005");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("ApiV1Group", error.GetMessage());
        Assert.Contains("GetProductsEndpoint", error.GetMessage());
    }

    [Fact]
    public void InvalidGroupType_WithoutIEndpointGroup_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api/v1"")]
public class ApiV1Group // Missing : IEndpointGroup
{
}

[MapGet(""/products"", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var error = Assert.Single(diagnostics, d => d.Id == "MINEP005");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
    }

    [Fact]
    public void InvalidGroupType_WithNeitherAttributeNorInterface_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

// Missing both [MapGroup] and IEndpointGroup
public class ApiV1Group
{
}

[MapGet(""/products"", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var error = Assert.Single(diagnostics, d => d.Id == "MINEP005");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
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
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}";
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
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}";
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
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGroup(""/v2"", ParentGroup = typeof(V1Group))]
public class V2Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}";
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
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGroup(""/products"", ParentGroup = typeof(V1Group))]
public class ProductsGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}";
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

        var minimalEndpointsAnalyzer = new MinimalEndpointsAnalyzer();
        var ambiguousRouteAnalyzer = new AmbiguousRouteAnalyzer();
        var groupHierarchyAnalyzer = new GroupHierarchyAnalyzer();
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
            minimalEndpointsAnalyzer,
            ambiguousRouteAnalyzer,
            groupHierarchyAnalyzer);

        var compilationWithAnalyzer = compilation.WithAnalyzers(analyzers);

        var diagnostics = compilationWithAnalyzer.GetAllDiagnosticsAsync().Result;

        return diagnostics
            .Where(d => d.Id.StartsWith("MINEP"))
            .ToList();
    }

}

