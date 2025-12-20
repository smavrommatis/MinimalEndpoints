using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using MinimalEndpoints.Analyzers.Models;
using MinimalEndpoints.Analyzers.Utilities;

namespace MinimalEndpoints.Analyzers.Tests.Utilities;

/// <summary>
/// Tests for EndpointCodeGenerator with hierarchical groups
/// </summary>
public class EndpointCodeGeneratorGroupHierarchyTests
{
    [Fact]
    public void GenerateCode_WithParentGroupNotDirectlyUsedByEndpoints_IncludesParentGroup()
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
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var endpoints = GetEndpoints(compilation);

        // Act
        var fileScope = EndpointCodeGenerator.GenerateCode("TestApp", "MinimalEndpointExtensions", endpoints);
        var generatedCode = fileScope.Build();

        // Assert
        Assert.NotNull(generatedCode);

        // Should include ApiGroup registration even though no endpoint directly uses it
        Assert.Contains("services.AddSingleton<TestApp.ApiGroup>();", generatedCode);
        Assert.Contains("services.AddSingleton<TestApp.V1Group>();", generatedCode);

        // Should include MapGroup__ApiGroup method
        Assert.Contains("public static RouteGroupBuilder MapGroup__TestApp_ApiGroup(", generatedCode);
        Assert.Contains("public static RouteGroupBuilder MapGroup__TestApp_V1Group(", generatedCode);

        // Should create ApiGroup in UseMinimalEndpoints
        Assert.Contains("var group_TestApp_ApiGroup = builder.MapGroup__TestApp_ApiGroup(app);", generatedCode);

        // Should pass ApiGroup to V1Group
        Assert.Contains("var group_TestApp_V1Group = builder.MapGroup__TestApp_V1Group(app, group_TestApp_ApiGroup);", generatedCode);
    }

    [Fact]
    public void GenerateCode_WithThreeLevelHierarchy_IncludesAllGroups()
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

[MapGroup(""/admin"", ParentGroup = typeof(V1Group))]
public class AdminGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGet(""/users"", Group = typeof(AdminGroup))]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var endpoints = GetEndpoints(compilation);

        // Act
        var fileScope = EndpointCodeGenerator.GenerateCode("TestApp", "MinimalEndpointExtensions", endpoints);
        var generatedCode = fileScope.Build();

        // Assert
        Assert.NotNull(generatedCode);

        // All three groups should be registered
        Assert.Contains("services.AddSingleton<TestApp.ApiGroup>();", generatedCode);
        Assert.Contains("services.AddSingleton<TestApp.V1Group>();", generatedCode);
        Assert.Contains("services.AddSingleton<TestApp.AdminGroup>();", generatedCode);

        // All three MapGroup methods should exist
        Assert.Contains("public static RouteGroupBuilder MapGroup__TestApp_ApiGroup(", generatedCode);
        Assert.Contains("public static RouteGroupBuilder MapGroup__TestApp_V1Group(", generatedCode);
        Assert.Contains("public static RouteGroupBuilder MapGroup__TestApp_AdminGroup(", generatedCode);

        // Should create groups in hierarchy order
        Assert.Contains("var group_TestApp_ApiGroup = builder.MapGroup__TestApp_ApiGroup(app);", generatedCode);
        Assert.Contains("var group_TestApp_V1Group = builder.MapGroup__TestApp_V1Group(app, group_TestApp_ApiGroup);", generatedCode);
        Assert.Contains("var group_TestApp_AdminGroup = builder.MapGroup__TestApp_AdminGroup(app, group_TestApp_V1Group);", generatedCode);

        // Verify order: ApiGroup appears before V1Group, V1Group before AdminGroup
        var apiGroupIndex = generatedCode.IndexOf("var group_TestApp_ApiGroup", StringComparison.Ordinal);
        var v1GroupIndex = generatedCode.IndexOf("var group_TestApp_V1Group", StringComparison.Ordinal);
        var adminGroupIndex = generatedCode.IndexOf("var group_TestApp_AdminGroup", StringComparison.Ordinal);

        Assert.True(apiGroupIndex < v1GroupIndex, "ApiGroup should be created before V1Group");
        Assert.True(v1GroupIndex < adminGroupIndex, "V1Group should be created before AdminGroup");
    }

    [Fact]
    public void GenerateCode_WithMultipleBranches_IncludesSharedParent()
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

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var endpoints = GetEndpoints(compilation);

        // Act
        var fileScope = EndpointCodeGenerator.GenerateCode("TestApp", "MinimalEndpointExtensions", endpoints);
        var generatedCode = fileScope.Build();

        // Assert
        Assert.NotNull(generatedCode);

        // Shared parent ApiGroup should be included
        Assert.Contains("services.AddSingleton<TestApp.ApiGroup>();", generatedCode);
        Assert.Contains("public static RouteGroupBuilder MapGroup__TestApp_ApiGroup(", generatedCode);
        Assert.Contains("var group_TestApp_ApiGroup = builder.MapGroup__TestApp_ApiGroup(app);", generatedCode);

        // Both child groups should reference the shared parent
        Assert.Contains("var group_TestApp_V1Group = builder.MapGroup__TestApp_V1Group(app, group_TestApp_ApiGroup);", generatedCode);
        Assert.Contains("var group_TestApp_V2Group = builder.MapGroup__TestApp_V2Group(app, group_TestApp_ApiGroup);", generatedCode);

        // ApiGroup should only be registered once
        var apiGroupRegistrations = System.Text.RegularExpressions.Regex.Matches(
            generatedCode,
            @"services\.AddSingleton<TestApp.ApiGroup>\(\);").Count;
        Assert.Equal(1, apiGroupRegistrations);
    }

    [Fact]
    public void GenerateCode_WithOnlyDirectGroups_WorksAsExpected()
    {
        // Arrange - No hierarchy, just direct groups
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGet(""/products"", Group = typeof(ApiGroup))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var endpoints = GetEndpoints(compilation);

        // Act
        var fileScope = EndpointCodeGenerator.GenerateCode("TestApp", "MinimalEndpointExtensions", endpoints);
        var generatedCode = fileScope.Build();

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("services.AddSingleton<TestApp.ApiGroup>();", generatedCode);
        Assert.Contains("public static RouteGroupBuilder MapGroup__TestApp_ApiGroup(", generatedCode);

        // Should NOT have parent parameter (root group)
        Assert.Contains("MapGroup__TestApp_ApiGroup(this IEndpointRouteBuilder builder, IApplicationBuilder app)", generatedCode);
    }

    private ImmutableArray<EndpointDefinition> GetEndpoints(Microsoft.CodeAnalysis.Compilation compilation)
    {
        var endpoints = new List<EndpointDefinition>();

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            foreach (var classDecl in root.DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>())
            {
                var symbol = semanticModel.GetDeclaredSymbol(classDecl);
                if (symbol is not null)
                {
                    var mapMethodsAttr = symbol.GetAttributes()
                        .FirstOrDefault(attr => attr.AttributeClass?.Name != null
                            && attr.AttributeClass.Name.Contains("Map")
                            && attr.AttributeClass.Name.Contains("Attribute")
                            && attr.AttributeClass.Name != "MapGroupAttribute");

                    if (mapMethodsAttr != null)
                    {
                        var mapMethodsDef = mapMethodsAttr.GetMapMethodAttributeDefinition();
                        if (mapMethodsDef != null)
                        {
                            var entryPoint = symbol.GetMembers()
                                .OfType<Microsoft.CodeAnalysis.IMethodSymbol>()
                                .FirstOrDefault(m => m.Name == (mapMethodsDef.EntryPoint ?? "HandleAsync")
                                    || m.Name == (mapMethodsDef.EntryPoint ?? "Handle"));

                            if (entryPoint != null)
                            {
                                var endpoint = EndpointDefinition.Create(symbol, entryPoint, mapMethodsDef);
                                endpoints.Add(endpoint);
                            }
                        }
                    }
                }
            }
        }

        return endpoints.ToImmutableArray();
    }
}

