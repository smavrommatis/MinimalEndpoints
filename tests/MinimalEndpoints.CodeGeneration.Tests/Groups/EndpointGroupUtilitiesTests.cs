using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Groups;
using MinimalEndpoints.CodeGeneration.Groups.Models;

namespace MinimalEndpoints.CodeGeneration.Tests.Groups;

public class EndpointGroupUtilitiesTests
{
    #region IsMapGroupAttribute Tests

    [Fact]
    public void IsMapGroupAttribute_WithMapGroupAttribute_ReturnsTrue()
    {
        // Arrange
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api"")]
public class ApiGroup { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("ApiGroup");
        var attribute = symbol!.GetAttributes().First();

        // Act
        var result = attribute.AttributeClass!.IsMapGroupAttribute();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMapGroupAttribute_WithMapGetAttribute_ReturnsFalse()
    {
        // Arrange
        var code = @"
using MinimalEndpoints.Annotations;

[MapGet(""/api"")]
public class TestEndpoint { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("TestEndpoint");
        var attribute = symbol!.GetAttributes().First();

        // Act
        var result = attribute.AttributeClass!.IsMapGroupAttribute();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMapGroupAttribute_WrongNamespace_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace CustomNamespace;

public class MapGroupAttribute : System.Attribute
{
    public MapGroupAttribute(string prefix) { }
}

[MapGroup(""/api"")]
public class ApiGroup { }";

        var compilation = new CompilationBuilder(code).Build();
        var symbol = compilation.GetTypeByMetadataName("CustomNamespace.ApiGroup");
        var attribute = symbol!.GetAttributes().First();

        // Act
        var result = attribute.AttributeClass!.IsMapGroupAttribute();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsConfigurableGroupEndpoint Tests

    [Fact]
    public void IsConfigurableGroupEndpoint_WithInterface_ReturnsTrue()
    {
        // Arrange
        var code = @"
using MinimalEndpoints;

namespace TestApp;

public class ApiGroup : IConfigurableGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.ApiGroup");

        // Act
        var result = symbol!.IsConfigurableGroupEndpoint();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsConfigurableGroupEndpoint_WithoutInterface_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace TestApp;

public class ApiGroup { }";

        var compilation = new CompilationBuilder(code).Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.ApiGroup");

        // Act
        var result = symbol!.IsConfigurableGroupEndpoint();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region FillHierarchyAndDetectCycles - Simple Hierarchy Tests

    [Fact]
    public void FillHierarchyAndDetectCycles_SingleGroup_NoParent()
    {
        // Arrange
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api"")]
public class ApiGroup { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("ApiGroup");
        var attribute = symbol!.GetAttributes().First();

        var group = new EndpointGroupDefinition(symbol, attribute);

        // Act
        var result = new[] { group }.FillHierarchyAndDetectCycles();

        // Assert
        Assert.Single(result);
        Assert.Null(result[symbol].ParentGroup);
        Assert.Empty(result[symbol].Cycles);
    }

    [Fact]
    public void FillHierarchyAndDetectCycles_TwoLevelHierarchy_SetsParentCorrectly()
    {
        // Arrange
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();

        var apiSymbol = compilation.GetTypeByMetadataName("ApiGroup");
        var v1Symbol = compilation.GetTypeByMetadataName("V1Group");

        var apiGroup = new EndpointGroupDefinition(apiSymbol!, apiSymbol!.GetAttributes().First());
        var v1Group = new EndpointGroupDefinition(v1Symbol!, v1Symbol!.GetAttributes().First());

        // Act
        var result = new[] { apiGroup, v1Group }.FillHierarchyAndDetectCycles();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Null(result[apiSymbol!].ParentGroup);
        Assert.NotNull(result[v1Symbol!].ParentGroup);
        Assert.Equal(apiSymbol, result[v1Symbol].ParentGroup!.Symbol, SymbolEqualityComparer.Default);
        Assert.Empty(result[apiSymbol].Cycles);
        Assert.Empty(result[v1Symbol].Cycles);
    }

    [Fact]
    public void FillHierarchyAndDetectCycles_ThreeLevelHierarchy_BuildsCorrectly()
    {
        // Arrange
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }

[MapGroup(""/users"", ParentGroup = typeof(V1Group))]
public class UsersGroup { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();

        var apiSymbol = compilation.GetTypeByMetadataName("ApiGroup");
        var v1Symbol = compilation.GetTypeByMetadataName("V1Group");
        var usersSymbol = compilation.GetTypeByMetadataName("UsersGroup");

        var apiGroup = new EndpointGroupDefinition(apiSymbol!, apiSymbol!.GetAttributes().First());
        var v1Group = new EndpointGroupDefinition(v1Symbol!, v1Symbol!.GetAttributes().First());
        var usersGroup = new EndpointGroupDefinition(usersSymbol!, usersSymbol!.GetAttributes().First());

        // Act
        var result = new[] { apiGroup, v1Group, usersGroup }.FillHierarchyAndDetectCycles();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Null(result[apiSymbol!].ParentGroup);
        Assert.Equal(apiSymbol, result[v1Symbol!].ParentGroup!.Symbol, SymbolEqualityComparer.Default);
        Assert.Equal(v1Symbol, result[usersSymbol!].ParentGroup!.Symbol, SymbolEqualityComparer.Default);
    }

    #endregion

    #region FillHierarchyAndDetectCycles - Cycle Detection Tests

    [Fact]
    public void FillHierarchyAndDetectCycles_SimpleCycle_DetectsAndBreaks()
    {
        // Arrange
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/a"", ParentGroup = typeof(GroupB))]
public class GroupA { }

[MapGroup(""/b"", ParentGroup = typeof(GroupA))]
public class GroupB { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();

        var groupASymbol = compilation.GetTypeByMetadataName("GroupA");
        var groupBSymbol = compilation.GetTypeByMetadataName("GroupB");

        var groupA = new EndpointGroupDefinition(groupASymbol!, groupASymbol!.GetAttributes().First());
        var groupB = new EndpointGroupDefinition(groupBSymbol!, groupBSymbol!.GetAttributes().First());

        // Act
        var result = new[] { groupA, groupB }.FillHierarchyAndDetectCycles();

        // Assert - Cycle should be detected and broken
        var hasCycle = result.Values.Any(g => g.Cycles.Count > 0);
        Assert.True(hasCycle);

        // One of the parent references should be nulled to break the cycle
        var nullParentCount = result.Values.Count(g => g.ParentGroup == null);
        Assert.True(nullParentCount > 0);
    }

    [Fact]
    public void FillHierarchyAndDetectCycles_SelfReferenceCycle_DetectsAndBreaks()
    {
        // Arrange
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api"", ParentGroup = typeof(ApiGroup))]
public class ApiGroup { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();

        var symbol = compilation.GetTypeByMetadataName("ApiGroup");

        var group = new EndpointGroupDefinition(symbol!, symbol!.GetAttributes().First());

        // Act
        var result = new[] { group }.FillHierarchyAndDetectCycles();

        // Assert
        Assert.Single(result);
        Assert.NotEmpty(result[symbol!].Cycles);
        Assert.Null(result[symbol].ParentGroup); // Self-reference broken
    }

    [Fact]
    public void FillHierarchyAndDetectCycles_ThreeGroupCycle_DetectsAndBreaks()
    {
        // Arrange
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/a"", ParentGroup = typeof(GroupC))]
public class GroupA { }

[MapGroup(""/b"", ParentGroup = typeof(GroupA))]
public class GroupB { }

[MapGroup(""/c"", ParentGroup = typeof(GroupB))]
public class GroupC { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();

        var groupASymbol = compilation.GetTypeByMetadataName("GroupA");
        var groupBSymbol = compilation.GetTypeByMetadataName("GroupB");
        var groupCSymbol = compilation.GetTypeByMetadataName("GroupC");

        var groupA = new EndpointGroupDefinition(groupASymbol!, groupASymbol!.GetAttributes().First());
        var groupB = new EndpointGroupDefinition(groupBSymbol!, groupBSymbol!.GetAttributes().First());
        var groupC = new EndpointGroupDefinition(groupCSymbol!, groupCSymbol!.GetAttributes().First());

        // Act
        var result = new[] { groupA, groupB, groupC }.FillHierarchyAndDetectCycles();

        // Assert
        var hasCycle = result.Values.Any(g => g.Cycles.Count > 0);
        Assert.True(hasCycle);
    }

    #endregion

    #region FillHierarchyAndDetectCycles - Diamond Pattern Tests

    [Fact]
    public void FillHierarchyAndDetectCycles_DiamondPattern_NoCycle()
    {
        // Arrange - A → B, A → C, B → D, C → D (Diamond, but no cycle)
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/root"")]
public class RootGroup { }

[MapGroup(""/b"", ParentGroup = typeof(RootGroup))]
public class GroupB { }

[MapGroup(""/c"", ParentGroup = typeof(RootGroup))]
public class GroupC { }

[MapGroup(""/d"", ParentGroup = typeof(GroupB))]
public class GroupD { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();

        var rootSymbol = compilation.GetTypeByMetadataName("RootGroup");
        var groupBSymbol = compilation.GetTypeByMetadataName("GroupB");
        var groupCSymbol = compilation.GetTypeByMetadataName("GroupC");
        var groupDSymbol = compilation.GetTypeByMetadataName("GroupD");

        var root = new EndpointGroupDefinition(rootSymbol!, rootSymbol!.GetAttributes().First());
        var groupB = new EndpointGroupDefinition(groupBSymbol!, groupBSymbol!.GetAttributes().First());
        var groupC = new EndpointGroupDefinition(groupCSymbol!, groupCSymbol!.GetAttributes().First());
        var groupD = new EndpointGroupDefinition(groupDSymbol!, groupDSymbol!.GetAttributes().First());

        // Act
        var result = new[] { root, groupB, groupC, groupD }.FillHierarchyAndDetectCycles();

        // Assert - No cycles in diamond pattern
        Assert.All(result.Values, g => Assert.Empty(g.Cycles));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void FillHierarchyAndDetectCycles_EmptyCollection_ReturnsEmptyDictionary()
    {
        // Act
        var result = Array.Empty<EndpointGroupDefinition>().FillHierarchyAndDetectCycles();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FillHierarchyAndDetectCycles_ParentNotInCollection_LeavesParentNull()
    {
        // Arrange
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();

        var v1Symbol = compilation.GetTypeByMetadataName("V1Group");

        var v1Group = new EndpointGroupDefinition(v1Symbol!, v1Symbol!.GetAttributes().First());

        // Act - Only include V1Group, not ApiGroup
        var result = new[] { v1Group }.FillHierarchyAndDetectCycles();

        // Assert
        Assert.Single(result);
        Assert.Null(result[v1Symbol!].ParentGroup); // Parent not found
    }

    [Fact]
    public void FillHierarchyAndDetectCycles_MultipleIndependentHierarchies_HandlesCorrectly()
    {
        // Arrange
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api1"")]
public class Api1Group { }

[MapGroup(""/v1"", ParentGroup = typeof(Api1Group))]
public class V1Group { }

[MapGroup(""/api2"")]
public class Api2Group { }

[MapGroup(""/v2"", ParentGroup = typeof(Api2Group))]
public class V2Group { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();

        var api1Symbol = compilation.GetTypeByMetadataName("Api1Group");
        var v1Symbol = compilation.GetTypeByMetadataName("V1Group");
        var api2Symbol = compilation.GetTypeByMetadataName("Api2Group");
        var v2Symbol = compilation.GetTypeByMetadataName("V2Group");

        var api1 = new EndpointGroupDefinition(api1Symbol!, api1Symbol!.GetAttributes().First());
        var v1 = new EndpointGroupDefinition(v1Symbol!, v1Symbol!.GetAttributes().First());
        var api2 = new EndpointGroupDefinition(api2Symbol!, api2Symbol!.GetAttributes().First());
        var v2 = new EndpointGroupDefinition(v2Symbol!, v2Symbol!.GetAttributes().First());

        // Act
        var result = new[] { api1, v1, api2, v2 }.FillHierarchyAndDetectCycles();

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Null(result[api1Symbol!].ParentGroup);
        Assert.Null(result[api2Symbol!].ParentGroup);
        Assert.Equal(api1Symbol, result[v1Symbol!].ParentGroup!.Symbol, SymbolEqualityComparer.Default);
        Assert.Equal(api2Symbol, result[v2Symbol!].ParentGroup!.Symbol, SymbolEqualityComparer.Default);
    }

    #endregion
}

