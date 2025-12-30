using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.IntegrationTests;

namespace MinimalEndpoints.CodeGeneration.IntegrationTests;

/// <summary>
/// Tests for IConditionallyMapped feature - endpoints and groups that can be conditionally mapped
/// </summary>
public class ConditionalMappingIntegrationTests
{
    #region Conditional Endpoints - Code Generation

    [Fact]
    public void GeneratedCode_ConditionalEndpoint_ContainsShouldMapCheck()
    {
        // Arrange
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/conditional"")]
public class ConditionalEndpoint : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;

    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("if (!TestApp.Endpoints.ConditionalEndpoint.ShouldMap(app))", generatedCode);
        Assert.Contains("return null;", generatedCode);
    }

    [Fact]
    public void GeneratedCode_ConditionalEndpoint_ReturnsNullableType()
    {
        // Arrange
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/conditional"")]
public class ConditionalEndpoint : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;

    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        // Mapping method should return IEndpointRouteBuilder? (nullable)
        Assert.Contains("private static IEndpointRouteBuilder? Map__TestApp_Endpoints_ConditionalEndpoint", generatedCode);
    }

    [Fact]
    public void GeneratedCode_NonConditionalEndpoint_DoesNotContainShouldMapCheck()
    {
        // Arrange
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/normal"")]
public class NormalEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.DoesNotContain("ShouldMap", generatedCode);
        // Return type should be non-nullable
        Assert.Contains("private static IEndpointRouteBuilder Map__TestApp_Endpoints_NormalEndpoint", generatedCode);
        Assert.DoesNotContain("IEndpointRouteBuilder?", generatedCode);
    }

    [Fact]
    public void GeneratedCode_ConditionalEndpoint_WithScopedLifetime_RegistersCorrectly()
    {
        // Arrange
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/conditional"", ServiceLifetime.Scoped)]
public class ConditionalEndpoint : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;

    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("services.AddScoped<TestApp.Endpoints.ConditionalEndpoint>", generatedCode);
        Assert.Contains("if (!TestApp.Endpoints.ConditionalEndpoint.ShouldMap(app))", generatedCode);
    }

    [Fact]
    public void GeneratedCode_ConditionalEndpoint_WithServiceType_UsesInterface()
    {
        // Arrange
        var code = @"
namespace TestApp.Endpoints;

public interface IConditionalService
{
    Task<IResult> HandleAsync();
}

[MapGet(""/conditional"", ServiceType = typeof(IConditionalService))]
public class ConditionalEndpoint : IConditionalService, IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;

    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.True(
            generatedCode.Contains("services.AddScoped<TestApp.Endpoints.IConditionalService, TestApp.Endpoints.ConditionalEndpoint>") ||
            generatedCode.Contains("services.AddScoped<IConditionalService, ConditionalEndpoint>"),
            "Should register with interface");
        Assert.Contains("ConditionalEndpoint.ShouldMap", generatedCode);
    }

    #endregion

    #region Conditional Groups - Code Generation

    [Fact]
    public void GeneratedCode_ConditionalGroup_ContainsShouldMapCheck()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
}

[MapGet(""/products"", Group = typeof(ApiGroup))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("if (!TestApp.ApiGroup.ShouldMap(app))", generatedCode);
        Assert.Contains("return null;", generatedCode);
    }

    [Fact]
    public void GeneratedCode_ConditionalGroup_ReturnsNullableType()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
}

[MapGet(""/products"", Group = typeof(ApiGroup))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        // Group mapping method should return RouteGroupBuilder? (nullable)
        Assert.Contains("private static RouteGroupBuilder? MapGroup__TestApp_ApiGroup", generatedCode);
    }

    [Fact]
    public void GeneratedCode_ConditionalGroup_ChildEndpointsHaveNullCheck()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
}

[MapGet(""/products"", Group = typeof(ApiGroup))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        // In UseMinimalEndpoints, should check if group is null
        Assert.Contains("if (group_TestApp_ApiGroup is not null)", generatedCode);
    }

    [Fact]
    public void GeneratedCode_ConditionalGroup_WithConfigurableGroup_CallsConfigureGroup()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup : IConditionallyMapped, IConfigurableGroup
{
    public static bool ShouldMap(IApplicationBuilder app) => true;

    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGet(""/products"", Group = typeof(ApiGroup))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("groupInstance.ConfigureGroup(group);", generatedCode);
        Assert.Contains("if (!TestApp.ApiGroup.ShouldMap(app))", generatedCode);
    }

    #endregion

    #region Hierarchical Conditional Mapping

    [Fact]
    public void GeneratedCode_HierarchyConditionallyMapped_ParentConditional_ChildHasNullCheck()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
}

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }

[MapGet(""/products"", Group = typeof(V1Group))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        // V1Group creation should check parent is not null
        Assert.Contains("group_TestApp_ApiGroup is null ? null", generatedCode);
        // Endpoint mapping should check V1Group is not null
        Assert.Contains("if (group_TestApp_V1Group is not null)", generatedCode);
    }

    [Fact]
    public void GeneratedCode_HierarchyConditionallyMapped_ThreeLevels_AllNullChecks()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
}

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }

[MapGroup(""/admin"", ParentGroup = typeof(V1Group))]
public class AdminGroup { }

[MapGet(""/users"", Group = typeof(AdminGroup))]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        // All levels should have conditional checks
        Assert.Contains("if (!TestApp.ApiGroup.ShouldMap(app))", generatedCode);
        Assert.Contains("group_TestApp_ApiGroup is null ? null", generatedCode);
        Assert.Contains("group_TestApp_V1Group is null ? null", generatedCode);
        Assert.Contains("if (group_TestApp_AdminGroup is not null)", generatedCode);
    }

    [Fact]
    public void GeneratedCode_MixedConditionalAndNonConditional_ParentNonConditionalChildConditional()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
}

[MapGet(""/products"", Group = typeof(V1Group))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        // Parent group is not conditional, so no check
        Assert.DoesNotContain("ApiGroup.ShouldMap", generatedCode);
        // Child group is conditional
        Assert.Contains("if (!TestApp.V1Group.ShouldMap(app))", generatedCode);
        // Endpoints should check child group
        Assert.Contains("if (group_TestApp_V1Group is not null)", generatedCode);
    }

    [Fact]
    public void GeneratedCode_ConditionalEndpoint_InConditionalGroup_BothChecksPresent()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
}

[MapGet(""/products"", Group = typeof(ApiGroup))]
public class GetProductsEndpoint : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;

    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        // Group check
        Assert.Contains("if (!TestApp.ApiGroup.ShouldMap(app))", generatedCode);
        // Endpoint check
        Assert.Contains("if (!TestApp.GetProductsEndpoint.ShouldMap(app))", generatedCode);
        // Both should have null checks
        Assert.Contains("if (group_TestApp_ApiGroup is not null)", generatedCode);
    }

    #endregion

    #region Multiple Conditional Groups

    [Fact]
    public void GeneratedCode_MultipleSeparateConditionalGroups_EachHasChecks()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api/v1"")]
public class V1Group : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
}

[MapGroup(""/api/v2"")]
public class V2Group : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
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
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        // Both groups should have their own checks
        Assert.Contains("if (!TestApp.V1Group.ShouldMap(app))", generatedCode);
        Assert.Contains("if (!TestApp.V2Group.ShouldMap(app))", generatedCode);
        // Both should have null checks for endpoints
        Assert.Contains("if (group_TestApp_V1Group is not null)", generatedCode);
        Assert.Contains("if (group_TestApp_V2Group is not null)", generatedCode);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GeneratedCode_ConditionalEndpoint_NoGroup_StillWorks()
    {
        // Arrange
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/standalone"")]
public class StandaloneConditionalEndpoint : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;

    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("if (!TestApp.Endpoints.StandaloneConditionalEndpoint.ShouldMap(app))", generatedCode);
        Assert.Contains("private static IEndpointRouteBuilder? Map__TestApp_Endpoints_StandaloneConditionalEndpoint", generatedCode);
    }

    [Fact]
    public void GeneratedCode_ConditionalGroup_NoEndpoints_StillGenerated()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
}";

        // Act
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // Assert
        Assert.NotNull(generatedCode);
        Assert.Contains("services.AddSingleton<TestApp.ApiGroup>", generatedCode);
        Assert.Contains("private static RouteGroupBuilder? MapGroup__TestApp_ApiGroup", generatedCode);
        Assert.Contains("if (!TestApp.ApiGroup.ShouldMap(app))", generatedCode);
    }

    #endregion
}

