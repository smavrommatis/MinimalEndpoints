using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.CodeGeneration.Tests.Groups.Analyzers;

public class GroupsAnalyzer_AmbiguousRoutesTests
{
    [Fact]
    public void WithDuplicateRoutes_ReportsWarning()
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
    public void WithSamePatternDifferentMethods_NoWarning()
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
    public void WithDifferentPatterns_NoWarning()
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
    public void WithCaseInsensitiveMatch_ReportsWarning()
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
    public void WithRouteConstraints_ReportsWarning()
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
    public void WithThreeConflicts_ReportsMultiple()
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

    [Fact]
    public void WithGroupHierarchy_DetectsConflicts()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }

[MapGet(""/products"", Group = typeof(V1Group))]
public class GetProductsV1Endpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/api/v1/products"")]
public class GetProductsV1DirectEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert - Both resolve to /api/v1/products
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void WithDifferentGroupHierarchies_NoConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }

[MapGroup(""/v2"", ParentGroup = typeof(ApiGroup))]
public class V2Group { }

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
        var diagnostics = GetDiagnostics(code);

        // Assert - /api/v1/products vs /api/v2/products - no conflict
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP004");
    }

    [Fact]
    public void WithTrailingSlash_DetectsConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/api/users"")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/api/users/"")]
public class GetUsersWithSlashEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void WithOptionalParameters_DetectsConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/users/{id?}"")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync(int? id) => Task.FromResult(Results.Ok());
}

[MapGet(""/users/{userId?}"")]
public class GetUsersByIdEndpoint
{
    public Task<IResult> HandleAsync(int? userId) => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void WithCatchAll_DetectsConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/{**path}"")]
public class CatchAllEndpoint1
{
    public Task<IResult> HandleAsync(string path) => Task.FromResult(Results.Ok());
}

[MapGet(""/{**route}"")]
public class CatchAllEndpoint2
{
    public Task<IResult> HandleAsync(string route) => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void DifferentConstraintsSamePattern_ReportsWarning()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/items/{id:int:min(1)}"")]
public class GetItemEndpoint
{
    public Task<IResult> HandleAsync(int id) => Task.FromResult(Results.Ok());
}

[MapGet(""/items/{id:int:max(1000)}"")]
public class GetItemWithMaxEndpoint
{
    public Task<IResult> HandleAsync(int id) => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }


    [Fact]
    public void WithNullPattern_DetectsConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(null)]
public class NullPatternEndpoint1
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(null)]
public class NullPatternEndpoint2
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert - Null patterns resolve to empty, should detect conflict
        var warnings = diagnostics.Where(d => d.Id == "MINEP004").ToList();
        Assert.NotEmpty(warnings);
    }

    [Fact]
    public void WithEmptyPattern_DetectsConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet("""")]
public class EmptyPatternEndpoint1
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet("""")]
public class EmptyPatternEndpoint2
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void WithNestedCatchAll_DetectsConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/files/{**path}"")]
public class FileEndpoint1
{
    public Task<IResult> HandleAsync(string path) => Task.FromResult(Results.Ok());
}

[MapGet(""/files/{**filepath}"")]
public class FileEndpoint2
{
    public Task<IResult> HandleAsync(string filepath) => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void WithMultipleConstraints_StillDetects()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/items/{id:int:range(1,100)}"")]
public class GetItemEndpoint1
{
    public Task<IResult> HandleAsync(int id) => Task.FromResult(Results.Ok());
}

[MapGet(""/items/{itemId:int:min(1):max(100)}"")]
public class GetItemEndpoint2
{
    public Task<IResult> HandleAsync(int itemId) => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert - Different constraint implementations but same pattern
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void MixedParameterAndLiteral_NoConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/users/admin"")]
public class GetAdminEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/users/{id}"")]
public class GetUserEndpoint
{
    public Task<IResult> HandleAsync(int id) => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert - Literal 'admin' vs parameter {id} - different patterns
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP004");
    }

    [Fact]
    public void DeepGroupHierarchy_DetectsConflicts()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }

[MapGroup(""/admin"", ParentGroup = typeof(V1Group))]
public class AdminGroup { }

[MapGet(""/users"", Group = typeof(AdminGroup))]
public class GetAdminUsersEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/api/v1/admin/users"")]
public class GetAdminUsersDirectEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert - Both resolve to /api/v1/admin/users
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void GroupWithTrailingSlash_NoConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api/"")]
public class ApiGroup { }

[MapGet(""/users"", Group = typeof(ApiGroup))]
public class GetUsersInGroupEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/api/users"")]
public class GetUsersDirectEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP004");
    }

    [Fact]
    public void EmptyGroupPrefix_DetectsConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup("""")]
public class EmptyPrefixGroup { }

[MapGet(""/users"", Group = typeof(EmptyPrefixGroup))]
public class GetUsersInGroupEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/users"")]
public class GetUsersDirectEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert - Both resolve to /users
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void WithDifferentParameterNames_DetectsConflict()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/api/users/{name}"")]
public class GetUserByNameEndpoint
{
    public Task<IResult> HandleAsync(string name) => Task.FromResult(Results.Ok());
}

[MapGet(""/api/users/{userName}"")]
public class GetUserByUserNameEndpoint
{
    public Task<IResult> HandleAsync(string userName) => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert - Both have same pattern structure
        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    private List<Diagnostic> GetDiagnostics(string code)
    {
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build(validateCompilation: false);

        return CompilationUtilities.GenerateDiagnostics(compilation);
    }

}

