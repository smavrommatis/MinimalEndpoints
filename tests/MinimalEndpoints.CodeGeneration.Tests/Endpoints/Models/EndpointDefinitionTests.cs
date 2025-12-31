using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;

namespace MinimalEndpoints.CodeGeneration.Tests.Endpoints.Models;

public class EndpointDefinitionTests
{
    [Fact]
    public void Factory_Predicate_WithMapGetAttribute_ReturnsTrue()
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
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Predicate(attribute);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Factory_Predicate_WithNonMapAttribute_ReturnsFalse()
    {
        // Arrange
        var code = @"
using System;

namespace TestApp;

[Obsolete]
public class TestEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Predicate(attribute);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Factory_Create_WithHandleMethod_ReturnsEndpointDefinition()
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
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Create(classSymbol, attribute);

        // Assert
        Assert.NotNull(result);
        var endpoint = result as EndpointDefinition;
        Assert.NotNull(endpoint);
        Assert.Equal("Handle", endpoint.EntryPoint.Name);
    }

    [Fact]
    public void Factory_Create_WithHandleAsyncMethod_ReturnsEndpointDefinition()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Create(classSymbol, attribute);

        // Assert
        Assert.NotNull(result);
        var endpoint = result as EndpointDefinition;
        Assert.NotNull(endpoint);
        Assert.Equal("HandleAsync", endpoint.EntryPoint.Name);
    }

    [Fact]
    public void Factory_Create_WithCustomEntryPoint_ReturnsEndpointDefinition()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"", EntryPoint = ""Process"")]
public class TestEndpoint
{
    public IResult Process() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Create(classSymbol, attribute);

        // Assert
        Assert.NotNull(result);
        var endpoint = result as EndpointDefinition;
        Assert.NotNull(endpoint);
        Assert.Equal("Process", endpoint.EntryPoint.Name);
    }

    [Fact]
    public void Factory_Create_WithMissingEntryPoint_ReturnsNull()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public void SomeOtherMethod() { }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Create(classSymbol, attribute);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void MappingEndpointMethodName_GeneratesCorrectName()
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
        var attribute = classSymbol.GetAttributes().First();
        var endpoint = EndpointDefinition.Factory.Create(classSymbol, attribute) as EndpointDefinition;

        // Act
        var result = endpoint!.MappingEndpointMethodName;

        // Assert
        Assert.Equal("Map__TestApp_TestEndpoint", result);
    }

    [Fact]
    public void MappingEndpointMethodName_WithDotsInNamespace_ReplacesWithUnderscores()
    {
        // Arrange
        var code = @"
namespace My.Test.App;

[MapGet(""/test"")]
public class TestEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");
        var attribute = classSymbol.GetAttributes().First();
        var endpoint = EndpointDefinition.Factory.Create(classSymbol, attribute) as EndpointDefinition;

        // Act
        var result = endpoint!.MappingEndpointMethodName;

        // Assert
        Assert.Equal("Map__My_Test_App_TestEndpoint", result);
        Assert.DoesNotContain(".", result);
    }

    [Fact]
    public void MappingEndpointMethodName_WithNestedTypes_ReplacesPlusWithUnderscores()
    {
        // Arrange
        var code = @"
namespace TestApp;

public class OuterClass
{
    [MapGet(""/test"")]
    public class TestEndpoint
    {
        public IResult Handle() => Results.Ok();
    }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetNestedClassSymbol(compilation, "OuterClass", "TestEndpoint");
        var attribute = classSymbol.GetAttributes().First();
        var endpoint = EndpointDefinition.Factory.Create(classSymbol, attribute) as EndpointDefinition;

        // Act
        var result = endpoint!.MappingEndpointMethodName;

        // Assert
        Assert.Contains("_OuterClass_TestEndpoint", result);
        Assert.DoesNotContain("+", result);
    }

    [Fact]
    public void MappingEndpointMethodName_CachesResult()
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
        var attribute = classSymbol.GetAttributes().First();
        var endpoint = EndpointDefinition.Factory.Create(classSymbol, attribute) as EndpointDefinition;

        // Act
        var result1 = endpoint!.MappingEndpointMethodName;
        var result2 = endpoint.MappingEndpointMethodName;

        // Assert
        Assert.Same(result1, result2);
    }

    [Fact]
    public void Create_WithConfigurableEndpoint_SetsIsConfigurable()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint : IConfigurableEndpoint
{
    public IResult Handle() => Results.Ok();

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder builder) { }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Create(classSymbol, attribute) as EndpointDefinition;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsConfigurable);
    }

    [Fact]
    public void Create_WithConditionallyMappedEndpoint_SetsIsConditionallyMapped()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint : IConditionallyMapped
{
    public IResult Handle() => Results.Ok();

    public static bool ShouldMap(IApplicationBuilder app) => true;
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Create(classSymbol, attribute) as EndpointDefinition;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsConditionallyMapped);
    }

    [Fact]
    public void Create_WithParameterAttributes_ExtractsAttributes()
    {
        // Arrange
        var code = @"
using System.ComponentModel.DataAnnotations;

namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public IResult Handle([Required] string name) => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Create(classSymbol, attribute) as EndpointDefinition;

        // Assert
        Assert.NotNull(result);
        var parameter = result.EntryPoint.Parameters["name"];
        Assert.NotEmpty(parameter.Attributes);
    }

    [Fact]
    public void Create_WithNullableParameter_SetsNullableFlag()
    {
        // Arrange
        var code = @"
namespace TestApp;

#nullable enable

[MapGet(""/test"")]
public class TestEndpoint
{
    public IResult Handle(string? name) => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Create(classSymbol, attribute) as EndpointDefinition;

        // Assert
        Assert.NotNull(result);
        var parameter = result.EntryPoint.Parameters["name"];
        Assert.True(parameter.Nullable);
    }

    [Fact]
    public void Create_WithDefaultValueParameter_CapturesDefaultValue()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public IResult Handle(int count = 10) => Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Create(classSymbol, attribute) as EndpointDefinition;

        // Assert
        Assert.NotNull(result);
        var parameter = result.EntryPoint.Parameters["count"];
        Assert.NotNull(parameter.DefaultValue);
        Assert.Equal("10", parameter.DefaultValue);
    }

    [Fact]
    public void Create_WithAsyncMethod_SetsIsAsyncFlag()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public async Task<IResult> Handle()
    {
        await Task.Delay(1);
        return Results.Ok();
    }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Create(classSymbol, attribute) as EndpointDefinition;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.EntryPoint.IsAsync);
    }

    [Fact]
    public void Create_WithTaskReturnType_SetsIsAsyncFlag()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/test"")]
public class TestEndpoint
{
    public Task<IResult> Handle() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestEndpoint");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Create(classSymbol, attribute) as EndpointDefinition;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.EntryPoint.IsAsync);
    }

    [Fact]
    public void ClassType_ReturnsTypeDefinition()
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
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Create(classSymbol, attribute) as EndpointDefinition;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ClassType);
        Assert.Contains("TestEndpoint", result.ClassType.FullName);
    }

    [Fact]
    public void MapMethodsAttribute_ReturnsAttributeDefinition()
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
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointDefinition.Factory.Create(classSymbol, attribute) as EndpointDefinition;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.MapMethodsAttribute);
        Assert.Contains("GET", result.MapMethodsAttribute.Methods);
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

    private static INamedTypeSymbol GetNestedClassSymbol(Compilation compilation, string outerClassName, string innerClassName)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        var outerClass = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == outerClassName);

        var innerClass = outerClass.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == innerClassName);

        return (semanticModel.GetDeclaredSymbol(innerClass) as INamedTypeSymbol)!;
    }
}

