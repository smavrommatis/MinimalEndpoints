using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Models;

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
    public void Constructor_StoresPredicate()
    {
        // Arrange
        Func<AttributeData, bool> predicate = _ => true;
        Func<INamedTypeSymbol, AttributeData, SymbolDefinition> create = (_, _) => null!;

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
        Func<INamedTypeSymbol, AttributeData, SymbolDefinition> create = (_, _) => null!;

        // Act
        var factory = new SymbolDefinitionFactory(predicate, create);

        // Assert
        Assert.Same(create, factory.Create);
    }

    private static INamedTypeSymbol GetClassSymbol(Compilation compilation, string className)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        var classDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == className);

        return (semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol)!;
    }
}

