using Microsoft.CodeAnalysis;
using static MinimalEndpoints.Tests.Common.CompilationUtilities;

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
    public void MissingEntryPoint_Message_NamesTheAttribute_NoBlankPlaceholder()
    {
        // The {1} placeholder names the attribute the class is marked with. It used to receive the
        // (null) custom EntryPoint, rendering "marked with  but" with a blank and a double space.
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public void SomeOtherMethod() { }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var error = Assert.Single(diagnostics, d => d.Id == "MINEP001");
        var message = error.GetMessage();
        Assert.Contains("[MapGet]", message);
        Assert.DoesNotContain("marked with  ", message);
    }

    [Fact]
    public void AnalyzeClassDeclaration_WithIncompleteAttribute_DoesNotCrash()
    {
        // '[MapGet]' with no pattern (mid-typing). The analyzer must not crash (AD0001) — the
        // compiler already reports the incomplete attribute. Before the fix, the unguarded
        // ConstructorArguments read throws and surfaces as AD0001 (which the MINEP filter would
        // hide), so this asserts on the full, unfiltered diagnostic set.
        var code = @"
namespace TestApp;

[MapGet]
public class TestEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build(validateCompilation: false);

        // Act — must complete without throwing.
        var diagnostics = GenerateAllDiagnostics(compilation);

        // Assert — no analyzer crash.
        Assert.DoesNotContain(diagnostics, d => d.Id == "AD0001");
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

        // Two endpoint attributes is NOT the endpoint+group combination — no MINEP007.
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP007");
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
    public void ServiceTypeValidation_WithInheritedInterfaceMember_NoError()
    {
        // The entry point is declared on a BASE interface that the ServiceType extends. Searching
        // only direct members (GetMembers) raised a false MINEP003; including AllInterfaces fixes it.
        var code = @"
namespace TestApp;

public interface IBaseEndpoint
{
    Task<IResult> HandleAsync();
}

public interface ITestEndpoint : IBaseEndpoint
{
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

    [Fact]
    public void InvalidGroupType_WithDuplicateMapGroupOnGroupType_DoesNotThrow()
    {
        // A group type with [MapGroup] duplicated across partial parts is a compiler error (CS0579),
        // but GetAttributes() still returns both. SingleOrDefault(predicate) throws on >1 match and
        // crashes the analyzer (AD0001). The group IS decorated (just erroneously twice), so MINEP005
        // must not fire either.
        var code = @"
namespace TestApp;

[MapGroup(""/a"")]
public partial class DupGroup : IConfigurableGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGroup(""/b"")]
public partial class DupGroup { }

[MapGet(""/products"", Group = typeof(DupGroup))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build(validateCompilation: false);

        // Act — must complete without throwing.
        var diagnostics = GenerateAllDiagnostics(compilation);

        // Assert — no analyzer crash, and no MINEP005 (the group IS decorated).
        Assert.DoesNotContain(diagnostics, d => d.Id == "AD0001");
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP005");
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
        // NOTE (pending product decision): the analyzer intentionally accepts a void-returning
        // handler, but the MINEP001 description (Diagnostics.cs) says a handler "should be
        // public, non-static, and return a value." This test pins current behaviour; the
        // analyzer-vs-description conflict is being escalated, not resolved here.
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

        // Three endpoint attributes, still no group — no MINEP007.
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP007");
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

        // A MapGet + MapMethods pairing is two endpoint attributes, not endpoint+group — no MINEP007.
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP007");
    }

    #endregion

    #region MINEP003 - Additional Edge Cases

    [Fact]
    public void ServiceTypeValidation_ParameterTypeMismatch_ReportsError()
    {
        // The interface declares HandleAsync(string) but the endpoint's selected entry point is
        // HandleAsync(int). The generator types the resolved instance as the ServiceType and calls
        // instance.HandleAsync(int), which would NOT bind against HandleAsync(string) — so MINEP003
        // must fire. Matching by name alone (ignoring the signature) let this miscompile through.
        var code = @"
namespace TestApp;

public interface ITestEndpoint
{
    Task<IResult> HandleAsync(string id); // string parameter
}

[MapGet(""/test"", ServiceType = typeof(ITestEndpoint))]
public class TestEndpoint
{
    public Task<IResult> HandleAsync(int id) // int parameter - incompatible with the interface
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var error = Assert.Single(diagnostics, d => d.Id == "MINEP003");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("HandleAsync", error.GetMessage());
    }

    [Fact]
    public void ServiceTypeValidation_CompatibleOverloadAmongSeveral_NoError()
    {
        // The interface offers two overloads; the endpoint's selected entry point matches one of
        // them exactly. Signature-aware validation must accept it (no MINEP003).
        var code = @"
namespace TestApp;

public interface ITestEndpoint
{
    Task<IResult> HandleAsync(string id);
    Task<IResult> HandleAsync(int id);
}

[MapGet(""/test"", ServiceType = typeof(ITestEndpoint))]
public class TestEndpoint : ITestEndpoint
{
    public Task<IResult> HandleAsync(string id) => Task.FromResult(Results.Ok());
    public Task<IResult> HandleAsync(int id) => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP003");
    }

    [Fact]
    public void ServiceTypeValidation_EntryPointOnBaseClass_NoError()
    {
        // ServiceType is a CLASS whose entry point is inherited from a base class. Searching only
        // the type's own members (and interfaces) but not its base chain raised a false MINEP003.
        // (Analyzer-focused: DI assignability is validated elsewhere.)
        var code = @"
namespace TestApp;

public abstract class BaseService
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

public class ConcreteService : BaseService { }

[MapGet(""/test"", ServiceType = typeof(ConcreteService))]
public class TestEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
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

    #region Partial-class de-duplication

    [Fact]
    public void MultipleAttributes_PartialClassThreeParts_ReportsOnce()
    {
        // Map attributes split across two parts, plus a third bare part. The merged symbol carries
        // both, so a per-part analyzer would report MINEP002 three times (including on the bare
        // part). It must be reported exactly once, on a Map-attributed part.
        var code = @"
namespace TestApp;

[MapGet(""/a"")]
public partial class E
{
    public IResult Handle() => Results.Ok();
}

[MapPost(""/a"")]
public partial class E
{
}

public partial class E
{
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.Single(diagnostics, d => d.Id == "MINEP002");
    }

    [Fact]
    public void MissingEntryPoint_PartialClass_ReportsOnce()
    {
        // A partial endpoint with the Map attribute on one part and no entry point. MINEP001 must
        // be reported once, not once per partial part.
        var code = @"
namespace TestApp;

[MapGet(""/a"")]
public partial class E
{
}

public partial class E
{
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.Single(diagnostics, d => d.Id == "MINEP001");
    }

    #endregion

    #region MINEP008 - Unsupported endpoint shape

    [Fact]
    public void GenericEndpoint_ReportsMinep008()
    {
        var code = @"
namespace TestApp;

[MapGet(""/items"")]
public class ListEndpoint<T>
{
    public IResult Handle() => Results.Ok();
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP008");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("ListEndpoint", warning.GetMessage());
    }

    [Fact]
    public void FileLocalEndpoint_ReportsMinep008()
    {
        var code = @"
namespace TestApp;

[MapGet(""/x"")]
file class Hidden
{
    public IResult Handle() => Results.Ok();
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.Single(diagnostics, d => d.Id == "MINEP008");
    }

    [Fact]
    public void PrivateNestedEndpoint_ReportsMinep008()
    {
        var code = @"
namespace TestApp;

public class Container
{
    [MapGet(""/x"")]
    private class Inner
    {
        public IResult Handle() => Results.Ok();
    }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.Single(diagnostics, d => d.Id == "MINEP008");
    }

    [Fact]
    public void ValidEndpoint_NoMinep008()
    {
        var code = @"
namespace TestApp;

[MapGet(""/x"")]
public class ValidEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP008");
    }

    [Fact]
    public void AbstractEndpoint_NoMinep008()
    {
        // Abstract endpoints are skipped silently (legitimate base pattern) — NOT reported.
        var code = @"
namespace TestApp;

[MapGet(""/x"")]
public abstract class BaseEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP008");
    }

    #endregion

}

