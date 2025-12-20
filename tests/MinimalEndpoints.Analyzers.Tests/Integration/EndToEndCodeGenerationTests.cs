using System.Collections.Immutable;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MinimalEndpoints.Analyzers.Models;
using MinimalEndpoints.Analyzers.Utilities;

namespace MinimalEndpoints.Analyzers.Tests.Integration;

/// <summary>
/// Integration tests that verify end-to-end code generation scenarios
/// </summary>
public class EndToEndCodeGenerationTests
{
    [Fact]
    public void GeneratedCode_CompilesSuccessfully_ForSimpleEndpoint()
    {
        // Arrange
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/test"")]
public class TestEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Microsoft.AspNetCore.Http.Results.Ok());
    }
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);

        Assert.Contains("public static IServiceCollection AddMinimalEndpoints", generatedCode);
        Assert.Contains("public static IEndpointRouteBuilder Map__TestApp_Endpoints_TestEndpoint", generatedCode);
        Assert.Contains("public static IApplicationBuilder UseMinimalEndpoints", generatedCode);
    }

    [Fact]
    public void GeneratedCode_HandlesGenericParameters_Correctly()
    {
        // Arrange
        var code = @"
namespace TestApp.Endpoints;

[MapPost(""/process"")]
public class ProcessEndpoint
{
    public Task<Dictionary<string, List<int>>> HandleAsync(Dictionary<string, object> data)
    {
        return Task.FromResult(new Dictionary<string, List<int>>());
    }
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("Dictionary<string, List<int>>", generatedCode);
        Assert.Contains("Dictionary<string, object>", generatedCode);
    }

    [Fact]
    public void GeneratedCode_HandlesMultipleAttributes_Correctly()
    {
        // Arrange
        var code = @"

namespace TestApp.Endpoints;

[MapPost(""/create"")]
public class CreateEndpoint
{
    public Task<IResult> HandleAsync(
        [FromBody][Required] string name,
        [FromQuery, Range(1, 100)] int age)
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("[FromBody]", generatedCode);
        Assert.Contains("[System.ComponentModel.DataAnnotations.Required]", generatedCode);
        Assert.Contains("[FromQuery]", generatedCode);
        Assert.Contains("[System.ComponentModel.DataAnnotations.Range(1, 100)]", generatedCode);
    }

    [Fact]
    public void GeneratedCode_HandlesConfigurableEndpoint_Correctly()
    {
        // Arrange
        var code = @"

namespace TestApp.Endpoints;

[MapGet(""/config"")]
public class ConfigurableEndpoint : IConfigurableEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }

    public static void Configure(IApplicationBuilder app, IEndpointConventionBuilder endpoint)
    {
        endpoint.WithTags(""test"");
    }
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("ConfigurableEndpoint.Configure(app, endpoint);", generatedCode);
    }

    [Fact]
    public void GeneratedCode_HandlesServiceNameParameter_Correctly()
    {
        // Arrange
        var code = @"
namespace TestApp.Endpoints;

public interface ITestService { Task<IResult> HandleAsync(); }

[MapGet(""/service"", ServiceType = typeof(ITestService))]
public class ServiceEndpoint : ITestService
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("ITestService", generatedCode);
        Assert.Contains("ServiceEndpoint", generatedCode);
    }

    [Fact]
    public void GeneratedCode_HandlesCustomEntryPoint_Correctly()
    {
        // Arrange
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/custom"", EntryPoint = ""CustomMethod"")]
public class CustomEntryPointEndpoint
{
    public Task<IResult> CustomMethod()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("CustomMethod", generatedCode);
    }

    [Fact]
    public void GeneratedCode_HandlesParameterNameCollision_WithEndpointInstance()
    {
        // Arrange - parameter named "endpointInstance" should be renamed
        var code = @"
using MinimalEndpoints.Annotations;

namespace TestApp.Endpoints;

[MapGet(""/collision"")]
public class CollisionEndpoint
{
    public Task<IResult> HandleAsync(string endpointInstance)
    {
        return Task.FromResult(Results.Ok(endpointInstance));
    }
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, diagnostics) = GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Should use endpointInstance1 for the injected service
        Assert.Contains("endpointInstance1", generatedCode);
        Assert.Contains("string endpointInstance", generatedCode);
    }

    [Fact]
    public void GeneratedCode_HandlesMultipleEndpoints_InSameFile()
    {
        // Arrange
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/endpoint1"")]
public class Endpoint1
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapPost(""/endpoint2"")]
public class Endpoint2
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapPut(""/endpoint3"")]
public class Endpoint3
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("Map__TestApp_Endpoints_Endpoint1", generatedCode);
        Assert.Contains("Map__TestApp_Endpoints_Endpoint2", generatedCode);
        Assert.Contains("Map__TestApp_Endpoints_Endpoint3", generatedCode);
        Assert.Contains("services.AddScoped<TestApp.Endpoints.Endpoint1>", generatedCode);
        Assert.Contains("services.AddScoped<TestApp.Endpoints.Endpoint2>", generatedCode);
        Assert.Contains("services.AddScoped<TestApp.Endpoints.Endpoint3>", generatedCode);
    }

    [Fact]
    public void GeneratedCode_HandlesDifferentLifetimes_Correctly()
    {
        // Arrange
        var code = @"
using MinimalEndpoints.Annotations;

namespace TestApp.Endpoints;

[MapGet(""/singleton"", ServiceLifetime.Singleton)]
public class SingletonEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/scoped"", ServiceLifetime.Scoped)]
public class ScopedEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/transient"", ServiceLifetime.Transient)]
public class TransientEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("services.AddSingleton<TestApp.Endpoints.SingletonEndpoint>", generatedCode);
        Assert.Contains("services.AddScoped<TestApp.Endpoints.ScopedEndpoint>", generatedCode);
        Assert.Contains("services.AddTransient<TestApp.Endpoints.TransientEndpoint>", generatedCode);
    }

    [Fact]
    public void GeneratedMethodName_ForNestedClass_UsesCorrectFormat()
    {
        // Arrange
        var code = @"
namespace TestApp.Endpoints;

public class OuterClass
{
    [MapGet(""/nested"")]
    public class NestedEndpoint
    {
        public Task<IResult> HandleAsync()
        {
            return Task.FromResult(Results.Ok());
        }
    }
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        // Nested class should use + separator which is replaced with _
        Assert.Contains("Map__TestApp_Endpoints_OuterClass_NestedEndpoint", generatedCode);
    }

    [Fact]
    public void GeneratedMethodName_ForLongNamespace_GeneratesCorrectly()
    {
        // Arrange
        var code = @"
namespace Very.Long.Deeply.Nested.Namespace.Structure.For.Testing.Purposes;

[MapGet(""/long"")]
public class LongNamespaceEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("Map__Very_Long_Deeply_Nested_Namespace_Structure_For_Testing_Purposes_LongNamespaceEndpoint", generatedCode);
    }

    [Fact]
    public void GeneratedMethodName_IsUnique_ForDifferentEndpoints()
    {
        // Arrange
        var code = @"
namespace TestApp.Api
{
    [MapGet(""/users"")]
    public class GetUsersEndpoint
    {
        public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
    }
}

namespace TestApp.Admin
{
    [MapGet(""/users"")]
    public class GetUsersEndpoint
    {
        public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
    }
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        // Both should have different method names due to different namespaces
        Assert.Contains("Map__TestApp_Api_GetUsersEndpoint", generatedCode);
        Assert.Contains("Map__TestApp_Admin_GetUsersEndpoint", generatedCode);

        // Verify they are registered separately
        Assert.Contains("services.AddScoped<TestApp.Api.GetUsersEndpoint>", generatedCode);
        Assert.Contains("services.AddScoped<TestApp.Admin.GetUsersEndpoint>", generatedCode);
    }

    [Fact]
    public void GeneratedMethodName_ForSpecialCharacters_EscapesCorrectly()
    {
        // Arrange - Test with underscores in namespace/class names
        var code = @"
namespace Test_App.End_Points;

[MapGet(""/test"")]
public class Test_Endpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        // Underscores should be preserved
        Assert.Contains("Map__Test_App_End_Points_Test_Endpoint", generatedCode);
    }

    [Fact]
    public void GeneratedCode_HandlesServiceTypeWithConfigurableEndpoint_Correctly()
    {
        // Arrange
        var code = @"
namespace TestApp.Endpoints;

public interface IConfigurableService
{
    Task<IResult> HandleAsync();
}

[MapGet(""/configurable-service"", ServiceType = typeof(IConfigurableService))]
public class ConfigurableServiceEndpoint : IConfigurableService, IConfigurableEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }

    public static void Configure(IApplicationBuilder app, IEndpointConventionBuilder endpoint)
    {
        endpoint.WithTags(""configured"");
    }
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);

        // Should register with interface
        Assert.True(
            generatedCode.Contains("AddScoped<TestApp.Endpoints.IConfigurableService, TestApp.Endpoints.ConfigurableServiceEndpoint>") ||
            generatedCode.Contains("AddScoped<IConfigurableService, ConfigurableServiceEndpoint>"),
            "Should contain service registration with interface"
        );

        // Should call Configure on the concrete class, not the interface
        Assert.True(
            generatedCode.Contains("ConfigurableServiceEndpoint.Configure(app, endpoint)") ||
            generatedCode.Contains("TestApp.Endpoints.ConfigurableServiceEndpoint.Configure(app, endpoint)"),
            "Should call Configure on concrete class"
        );
        Assert.DoesNotContain("IConfigurableService.Configure", generatedCode);
    }

    private (string generatedCode, IEnumerable<Diagnostic> diagnostics) GenerateCodeAndCompile(
        CSharpCompilation compilation,
        bool validateCompilation = true
        )
    {
        // Get all endpoint classes
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var endpointClasses = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .Select(classDecl => semanticModel.GetDeclaredSymbol(classDecl))
            .Where(symbol => symbol != null)
            .Select(symbol =>
            {
                var attr = symbol!.GetMapMethodAttributeDefinition();
                if (attr == null)
                {
                    return null;
                }

                var entryPoint = symbol.FindEntryPointMethod(attr.EntryPoint);
                return entryPoint == null
                    ? null
                    : EndpointDefinition.Create(symbol, entryPoint, attr);
            })
            .Where(ed => ed != null)
            .ToImmutableArray();

        if (endpointClasses.IsEmpty)
        {
            return (null!, Enumerable.Empty<Diagnostic>());
        }

        // Generate code
        var fileScope = EndpointCodeGenerator.GenerateCode(
            "MinimalEndpoints.Generated",
            "MinimalEndpointExtensions",
            endpointClasses
        );

        var generatedCode = fileScope?.Build();

        // Compile generated code with original to verify it compiles
        if (generatedCode != null)
        {
            var generatedTree = CSharpSyntaxTree.ParseText(generatedCode);
            var newCompilation = compilation.AddSyntaxTrees(generatedTree);
            var diagnostics = newCompilation.GetDiagnostics();

            if (validateCompilation)
            {
                var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

                if (errors.Length > 0)
                {
                    var errorMessages = string.Join(Environment.NewLine,
                        errors.Select(e => $"{e.Id}: {e.GetMessage()}"));
                    throw new InvalidOperationException(
                        $"Compilation failed with {errors.Length} error(s):{Environment.NewLine}{errorMessages}");
                }
            }

            return (generatedCode, diagnostics);
        }

        return (null!, compilation.GetDiagnostics());
    }
}

