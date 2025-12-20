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

    #endregion

    #region Helper Methods

    private List<Diagnostic> GetDiagnostics(string code)
    {
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build(validateCompilation: false);

        var minimalEndpointsAnalyzer = new MinimalEndpointsAnalyzer();
        var ambiguousRouteAnalyzer = new AmbiguousRouteAnalyzer();
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(minimalEndpointsAnalyzer, ambiguousRouteAnalyzer);

        var compilationWithAnalyzer = compilation.WithAnalyzers(analyzers);

        var diagnostics = compilationWithAnalyzer.GetAllDiagnosticsAsync().Result;

        return diagnostics
            .Where(d => d.Id.StartsWith("MINEP"))
            .ToList();
    }

    #endregion
}

