namespace MinimalEndpoints.CodeGeneration.IntegrationTests;

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
public class ApiGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) { }
}

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) { }
}

[MapGet(""/products"", Group = typeof(V1Group))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        // Act
        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);

        // Groups are no longer registered in DI; ConfigureGroup is invoked statically.
        Assert.DoesNotContain("services.AddSingleton<TestApp.ApiGroup>", generatedCode);
        Assert.DoesNotContain("services.AddSingleton<TestApp.V1Group>", generatedCode);
        Assert.Contains("TestApp.ApiGroup.ConfigureGroup(app, group);", generatedCode);
        Assert.Contains("TestApp.V1Group.ConfigureGroup(app, group);", generatedCode);

        // Should include MapGroup__ApiGroup method
        Assert.Contains("private static RouteGroupBuilder MapGroup__TestApp_ApiGroup(", generatedCode);
        Assert.Contains("private static RouteGroupBuilder MapGroup__TestApp_V1Group(", generatedCode);

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
public class ApiGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) { }
}

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) { }
}

[MapGroup(""/admin"", ParentGroup = typeof(V1Group))]
public class AdminGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) { }
}

[MapGet(""/users"", Group = typeof(AdminGroup))]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        // Act
        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);

        // No group is registered in DI; each configurable group is configured statically.
        Assert.DoesNotContain("services.AddSingleton<TestApp.ApiGroup>", generatedCode);
        Assert.DoesNotContain("services.AddSingleton<TestApp.V1Group>", generatedCode);
        Assert.DoesNotContain("services.AddSingleton<TestApp.AdminGroup>", generatedCode);
        Assert.Contains("TestApp.ApiGroup.ConfigureGroup(app, group);", generatedCode);
        Assert.Contains("TestApp.V1Group.ConfigureGroup(app, group);", generatedCode);
        Assert.Contains("TestApp.AdminGroup.ConfigureGroup(app, group);", generatedCode);

        // All three MapGroup methods should exist
        Assert.Contains("private static RouteGroupBuilder MapGroup__TestApp_ApiGroup(", generatedCode);
        Assert.Contains("private static RouteGroupBuilder MapGroup__TestApp_V1Group(", generatedCode);
        Assert.Contains("private static RouteGroupBuilder MapGroup__TestApp_AdminGroup(", generatedCode);

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
public class ApiGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) { }
}

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) { }
}

[MapGroup(""/v2"", ParentGroup = typeof(ApiGroup))]
public class V2Group : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) { }
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

        // Act
        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);

        // Shared parent ApiGroup should be included (no DI registration anymore)
        Assert.DoesNotContain("services.AddSingleton<TestApp.ApiGroup>", generatedCode);
        Assert.Contains("private static RouteGroupBuilder MapGroup__TestApp_ApiGroup(", generatedCode);
        Assert.Contains("var group_TestApp_ApiGroup = builder.MapGroup__TestApp_ApiGroup(app);", generatedCode);

        // Both child groups should reference the shared parent
        Assert.Contains("var group_TestApp_V1Group = builder.MapGroup__TestApp_V1Group(app, group_TestApp_ApiGroup);", generatedCode);
        Assert.Contains("var group_TestApp_V2Group = builder.MapGroup__TestApp_V2Group(app, group_TestApp_ApiGroup);", generatedCode);

        // The shared parent's group method (and its single ConfigureGroup call) is emitted once.
        var apiGroupConfigureCalls = System.Text.RegularExpressions.Regex.Matches(
            generatedCode,
            @"TestApp\.ApiGroup\.ConfigureGroup\(app, group\);").Count;
        Assert.Equal(1, apiGroupConfigureCalls);
    }

    [Fact]
    public void GenerateCode_WithOnlyDirectGroups_WorksAsExpected()
    {
        // Arrange - No hierarchy, just direct groups
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) { }
}

[MapGet(""/products"", Group = typeof(ApiGroup))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        // Act
        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.DoesNotContain("services.AddSingleton<TestApp.ApiGroup>", generatedCode);
        Assert.Contains("TestApp.ApiGroup.ConfigureGroup(app, group);", generatedCode);
        Assert.Contains("private static RouteGroupBuilder MapGroup__TestApp_ApiGroup(", generatedCode);

        // Should NOT have parent parameter (root group)
        Assert.Contains("MapGroup__TestApp_ApiGroup(this IEndpointRouteBuilder builder, IApplicationBuilder app)", generatedCode);
    }
}

