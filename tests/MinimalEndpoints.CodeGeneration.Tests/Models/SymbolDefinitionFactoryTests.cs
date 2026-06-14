using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Models;
using static MinimalEndpoints.Tests.Common.SymbolTestHelpers;

namespace MinimalEndpoints.CodeGeneration.Tests.Models;

public class SymbolDefinitionFactoryTests
{
    [Fact]
    public void TryCreateSymbol_WithMapGetAttribute_ReturnsEndpointDefinition()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MinimalEndpoints.CodeGeneration.Endpoints.Models.EndpointDefinition>(result);
    }

    [Fact]
    public void TryCreateSymbol_WithDuplicateParameterName_DoesNotThrow()
    {
        // A transient mid-edit state (duplicating a parameter while refactoring the Handle
        // signature; the file already has CS0100) used to throw ArgumentException from
        // .ToDictionary(x => x.Name) inside the transform — surfacing as AD0001 and dropping
        // generation for EVERY endpoint in the compilation. Discovery must never throw.
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public IResult Handle(int id, int id) => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build(validateCompilation: false);
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");

        // Act — must not throw.
        var exception = Record.Exception(() => SymbolDefinitionFactory.TryCreateSymbol(classSymbol));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void TryCreateSymbol_WithMultipleEndpointAttributes_DoesNotThrow()
    {
        // Two endpoint attributes on one class is ambiguous. Discovery must return null (no throw);
        // the generator skips the class and MINEP002 reports the error.
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
[MapPost(""/test"")]
public class TestEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");

        // Act — must not throw.
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryCreateSymbol_WithEndpointAndGroupAttributes_DoesNotThrow()
    {
        // An endpoint attribute AND a group attribute on one class — the MINEP007 case. Discovery
        // returns null (no throw); the analyzer reports MINEP007 via an explicit classification.
        var code = @"
namespace TestApp;

[MapGet(""/users"")]
[MapGroup(""/api"")]
public class TestEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");

        // Act — must not throw.
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryCreateSymbol_WithIncompleteMapAttribute_ReturnsNull()
    {
        // Arrange — '[MapGet]' mid-typing (no pattern). Discovery must degrade gracefully
        // (return null) rather than throw and crash the generator transform.
        var code = @"
namespace TestApp;

[MapGet]
public class TestEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build(validateCompilation: false);
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");

        // Act — must not throw.
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryCreateSymbol_WithMapPostAttribute_ReturnsEndpointDefinition()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapPost(""/test"")]
public class TestEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MinimalEndpoints.CodeGeneration.Endpoints.Models.EndpointDefinition>(result);
    }

    [Fact]
    public void TryCreateSymbol_WithMapPutAttribute_ReturnsEndpointDefinition()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapPut(""/test"")]
public class TestEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MinimalEndpoints.CodeGeneration.Endpoints.Models.EndpointDefinition>(result);
    }

    [Fact]
    public void TryCreateSymbol_WithMapDeleteAttribute_ReturnsEndpointDefinition()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapDelete(""/test"")]
public class TestEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MinimalEndpoints.CodeGeneration.Endpoints.Models.EndpointDefinition>(result);
    }

    [Fact]
    public void TryCreateSymbol_WithMapPatchAttribute_ReturnsEndpointDefinition()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapPatch(""/test"")]
public class TestEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MinimalEndpoints.CodeGeneration.Endpoints.Models.EndpointDefinition>(result);
    }

    [Fact]
    public void TryCreateSymbol_WithMapGroupAttribute_ReturnsGroupDefinition()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class TestGroup
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestGroup");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MinimalEndpoints.CodeGeneration.Groups.Models.EndpointGroupDefinition>(result);
    }

    [Fact]
    public void TryCreateSymbol_WithNoMapAttributes_ReturnsNull()
    {
        // Arrange
        var code = @"
namespace TestApp;

public class RegularClass
{
    public void Method() { }
}";

        var compilation = new CompilationBuilder(code).Build();
        var classSymbol = GetClassSymbol(compilation, "RegularClass");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryCreateSymbol_WithNonMapAttribute_ReturnsNull()
    {
        // Arrange
        var code = @"
using System;

namespace TestApp;

[Obsolete]
public class ObsoleteClass
{
    public void Method() { }
}";

        var compilation = new CompilationBuilder(code).Build();
        var classSymbol = GetClassSymbol(compilation, "ObsoleteClass");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryCreateSymbol_WithMissingEntryPoint_ReturnsNull()
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

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryCreateSymbol_WithMapMethodsAttribute_ReturnsEndpointDefinition()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapMethods(""/test"", new[] { ""GET"", ""POST"" })]
public class TestEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MinimalEndpoints.CodeGeneration.Endpoints.Models.EndpointDefinition>(result);
    }

    [Fact]
    public void TryCreateSymbol_WithAbstractClass_ReturnsNull()
    {
        // Abstract endpoint classes are a legitimate base-class pattern and are never mapped, so
        // discovery must skip them (no DI registration of an uninstantiable type).
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public abstract class BaseEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "BaseEndpoint");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryCreateSymbol_WithAbstractModifierOnOtherPartialPart_ReturnsNull()
    {
        // The `abstract` modifier is declared on a DIFFERENT partial part than the [MapGet]. The
        // merged symbol is abstract, so the symbol-level gate must skip it — the old syntax-level
        // filter (which inspected only the attributed part) would have missed this and generated a
        // registration for an abstract type.
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public partial class SplitEndpoint
{
    public IResult Handle() => Results.Ok();
}

public abstract partial class SplitEndpoint
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "SplitEndpoint");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryCreateSymbol_WithGenericEndpointClass_ReturnsNull()
    {
        // An open generic endpoint cannot be emitted: the method name would contain '<'/'>' and the
        // DI registration would have an unbound type parameter. The gate skips it (MINEP008).
        var code = @"
namespace TestApp;

[MapGet(""/items"")]
public class ListEndpoint<T>
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "ListEndpoint");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryCreateSymbol_WithPrivateNestedEndpoint_ReturnsNull()
    {
        // A private nested class is not referenceable from the generated (same-assembly) code.
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

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetNestedClassSymbol(compilation, "Container", "Inner");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryCreateSymbol_WithFileLocalEndpoint_ReturnsNull()
    {
        // A file-local type cannot be referenced from the generated file.
        var code = @"
namespace TestApp;

[MapGet(""/x"")]
file class Hidden
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "Hidden");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryCreateSymbol_WithInternalEndpoint_ReturnsDefinition()
    {
        // Generated code lives in the same assembly, so an internal endpoint IS referenceable and
        // must NOT be rejected (guards against the accessibility gate being too strict).
        var code = @"
namespace TestApp;

[MapGet(""/x"")]
internal class InternalEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "InternalEndpoint");

        // Act
        var result = SymbolDefinitionFactory.TryCreateSymbol(classSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MinimalEndpoints.CodeGeneration.Endpoints.Models.EndpointDefinition>(result);
    }

    [Fact]
    public void Constructor_StoresPredicate()
    {
        // Arrange
        Func<AttributeData, bool> predicate = _ => true;
        Func<INamedTypeSymbol, AttributeData, AccessibilityScope, SymbolDefinition> create = (_, _, _) => null!;

        // Act
        var factory = new SymbolDefinitionFactory(predicate, create);

        // Assert
        Assert.Same(predicate, factory.Predicate);
    }

    [Fact]
    public void Constructor_StoresCreateFunction()
    {
        // Arrange
        Func<AttributeData, bool> predicate = _ => true;
        Func<INamedTypeSymbol, AttributeData, AccessibilityScope, SymbolDefinition> create = (_, _, _) => null!;

        // Act
        var factory = new SymbolDefinitionFactory(predicate, create);

        // Assert — the Create method delegates to the supplied function (returns its result).
        Assert.Null(factory.Create(null!, null!));
    }
}

