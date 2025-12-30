using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.CodeGeneration.Tests.Endpoints.Analyzers;

/// <summary>
/// Tests for the MinimalEndpointsAnalyzer diagnostic analyzer.
/// </summary>
public class EndpointsAnalyzerTests
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
    public void MissingEntryPoint_WithHandleAsyncMethod_NoError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public Task<IResult> HandleAsync()
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

    #region MINEP005 - Invalid Group Type Tests

    [Fact]
    public void InvalidGroupType_WithValidGroup_NoError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api/v1"")]
public class ApiV1Group : IConfigurableGroup
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
public class ApiV1Group : IConfigurableGroup
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
    public void InvalidGroupType_WithNeitherAttributeNorInterface_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

// Missing both [MapGroup] and IConfigurableGroup
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

    #region MINEP001 - Additional Edge Cases

    [Fact]
    public void MissingEntryPoint_PrivateMethod_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    private Task<IResult> HandleAsync()
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
    public void MissingEntryPoint_StaticMethod_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public static Task<IResult> HandleAsync()
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
    public void MissingEntryPoint_PropertyNotMethod_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public IResult Handle => Results.Ok();
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var error = Assert.Single(diagnostics, d => d.Id == "MINEP001");
    }

    [Fact]
    public void MissingEntryPoint_MethodWithVoidReturn_NoError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public void HandleAsync() { }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void MissingEntryPoint_InheritedMethod_NoError()
    {
        // Arrange
        var code = @"
namespace TestApp;

public abstract class BaseEndpoint
{
    public abstract Task<IResult> HandleAsync();
}

[MapGet(""/test"")]
public class TestEndpoint : BaseEndpoint
{
    public override Task<IResult> HandleAsync()
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
    public void MissingEntryPoint_OverloadedMethods_SelectsHandleAsync()
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

    #endregion

    #region MINEP002 - Additional Edge Cases

    [Fact]
    public void MultipleAttributes_ThreeAttributes_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
[MapPost(""/test"")]
[MapPut(""/test"")]
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
    }

    [Fact]
    public void MultipleAttributes_MapGetAndMapMethods_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
[MapMethods(""/test"", new[] { ""POST"" })]
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
    }

    #endregion

    #region MINEP003 - Additional Edge Cases

    [Fact]
    public void ServiceTypeValidation_ParameterTypeMismatch_InterfaceStillValidated()
    {
        // Arrange
        var code = @"
namespace TestApp;

public interface ITestEndpoint
{
    Task<IResult> HandleAsync(string id); // string parameter
}

[MapGet(""/test"", ServiceType = typeof(ITestEndpoint))]
public class TestEndpoint : ITestEndpoint
{
    public Task<IResult> HandleAsync(int id) // int parameter - mismatch
    {
        return Task.FromResult(Results.Ok());
    }

    public Task<IResult> HandleAsync(string id)
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        // Interface has the method HandleAsync, so no MINEP003 error
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP003");
    }

    [Fact]
    public void ServiceTypeValidation_GenericInterface_ValidatesCorrectly()
    {
        // Arrange
        var code = @"
namespace TestApp;

public interface ITestEndpoint<T>
{
    Task<IResult> HandleAsync(T data);
}

[MapGet(""/test"", ServiceType = typeof(ITestEndpoint<string>))]
public class TestEndpoint : ITestEndpoint<string>
{
    public Task<IResult> HandleAsync(string data)
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP003");
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

