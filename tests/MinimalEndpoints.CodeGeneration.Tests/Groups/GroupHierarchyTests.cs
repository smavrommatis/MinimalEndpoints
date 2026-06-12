using MinimalEndpoints.CodeGeneration.Groups;
using MinimalEndpoints.CodeGeneration.Groups.Models;

namespace MinimalEndpoints.CodeGeneration.Tests.Groups;

/// <summary>
/// Covers the transient, FQN-keyed group hierarchy that replaced the mutable
/// FillHierarchyAndDetectCycles + memoized hierarchy state on the cached models. Resolution is by
/// fully-qualified name (the group classes here are top-level, so the FQN is the simple name).
/// </summary>
public class GroupHierarchyTests
{
    private static EndpointGroupDefinition Group(Microsoft.CodeAnalysis.Compilation compilation, string name)
    {
        var symbol = compilation.GetTypeByMetadataName(name)!;
        return new EndpointGroupDefinition(symbol, symbol.GetAttributes().First());
    }

    #region Simple Hierarchy

    [Fact]
    public void Build_SingleGroup_NoParent()
    {
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api"")]
public class ApiGroup { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var api = Group(compilation, "ApiGroup");

        var hierarchy = GroupHierarchy.Build(new[] { api });

        Assert.Equal(1, hierarchy.Count);
        Assert.True(hierarchy.TryGet("ApiGroup", out _));
        Assert.Null(hierarchy.Parent(api));
        Assert.Empty(hierarchy.Cycles);
        Assert.Equal(1, hierarchy.DepthOf(api));
        Assert.Equal("/api", hierarchy.FullPrefixOf(api));
    }

    [Fact]
    public void Build_TwoLevelHierarchy_ResolvesParentByName()
    {
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var api = Group(compilation, "ApiGroup");
        var v1 = Group(compilation, "V1Group");

        var hierarchy = GroupHierarchy.Build(new[] { api, v1 });

        Assert.Equal(2, hierarchy.Count);
        Assert.Null(hierarchy.Parent(api));
        Assert.NotNull(hierarchy.Parent(v1));
        Assert.Equal("ApiGroup", hierarchy.Parent(v1)!.ClassType.FullName);
        Assert.Equal(1, hierarchy.DepthOf(api));
        Assert.Equal(2, hierarchy.DepthOf(v1));
        Assert.Equal("/api/v1", hierarchy.FullPrefixOf(v1));
        Assert.Empty(hierarchy.Cycles);
    }

    [Fact]
    public void Build_ThreeLevelHierarchy_BuildsCorrectly()
    {
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }

[MapGroup(""/users"", ParentGroup = typeof(V1Group))]
public class UsersGroup { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var api = Group(compilation, "ApiGroup");
        var v1 = Group(compilation, "V1Group");
        var users = Group(compilation, "UsersGroup");

        var hierarchy = GroupHierarchy.Build(new[] { api, v1, users });

        Assert.Equal(3, hierarchy.Count);
        Assert.Null(hierarchy.Parent(api));
        Assert.Equal("ApiGroup", hierarchy.Parent(v1)!.ClassType.FullName);
        Assert.Equal("V1Group", hierarchy.Parent(users)!.ClassType.FullName);
        Assert.Equal(3, hierarchy.DepthOf(users));
        Assert.Equal("/api/v1/users", hierarchy.FullPrefixOf(users));
    }

    #endregion

    #region Cycle Detection

    [Fact]
    public void Build_SimpleCycle_DetectsAndBreaks()
    {
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/a"", ParentGroup = typeof(GroupB))]
public class GroupA { }

[MapGroup(""/b"", ParentGroup = typeof(GroupA))]
public class GroupB { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var a = Group(compilation, "GroupA");
        var b = Group(compilation, "GroupB");

        var hierarchy = GroupHierarchy.Build(new[] { a, b });

        Assert.NotEmpty(hierarchy.Cycles);
        // At least one parent edge must be broken to break the cycle.
        var nullParents = hierarchy.Groups.Count(g => hierarchy.Parent(g) == null);
        Assert.True(nullParents > 0);
    }

    [Fact]
    public void Build_SelfReferenceCycle_DetectsAndBreaks()
    {
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api"", ParentGroup = typeof(ApiGroup))]
public class ApiGroup { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var api = Group(compilation, "ApiGroup");

        var hierarchy = GroupHierarchy.Build(new[] { api });

        Assert.Single(hierarchy.Groups);
        Assert.NotEmpty(hierarchy.Cycles);
        Assert.Null(hierarchy.Parent(api));
    }

    [Fact]
    public void Build_SelfReferenceCycle_RecordsCycleNames()
    {
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api"", ParentGroup = typeof(ApiGroup))]
public class ApiGroup { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var api = Group(compilation, "ApiGroup");

        var hierarchy = GroupHierarchy.Build(new[] { api });

        var cycle = Assert.Single(hierarchy.Cycles);
        Assert.Equal("ApiGroup", cycle.Group.Name);
        // The recorded path includes the repeated node, matching the old symbol-based behaviour.
        Assert.Equal(new[] { "ApiGroup", "ApiGroup" }, cycle.Names);
    }

    [Fact]
    public void Build_ThreeGroupCycle_DetectsAndBreaks()
    {
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/a"", ParentGroup = typeof(GroupC))]
public class GroupA { }

[MapGroup(""/b"", ParentGroup = typeof(GroupA))]
public class GroupB { }

[MapGroup(""/c"", ParentGroup = typeof(GroupB))]
public class GroupC { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var a = Group(compilation, "GroupA");
        var b = Group(compilation, "GroupB");
        var c = Group(compilation, "GroupC");

        var hierarchy = GroupHierarchy.Build(new[] { a, b, c });

        Assert.NotEmpty(hierarchy.Cycles);
    }

    #endregion

    #region Diamond and Edge Cases

    [Fact]
    public void Build_DiamondPattern_NoCycle()
    {
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
        var root = Group(compilation, "RootGroup");
        var b = Group(compilation, "GroupB");
        var c = Group(compilation, "GroupC");
        var d = Group(compilation, "GroupD");

        var hierarchy = GroupHierarchy.Build(new[] { root, b, c, d });

        Assert.Empty(hierarchy.Cycles);
    }

    [Fact]
    public void Build_EmptyCollection_ReturnsEmptyHierarchy()
    {
        var hierarchy = GroupHierarchy.Build(Array.Empty<EndpointGroupDefinition>());

        Assert.Equal(0, hierarchy.Count);
        Assert.Empty(hierarchy.Groups);
        Assert.Empty(hierarchy.Cycles);
    }

    [Fact]
    public void Build_ParentNotInCollection_LeavesParentNull()
    {
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var v1 = Group(compilation, "V1Group");

        // Only include V1Group, not ApiGroup.
        var hierarchy = GroupHierarchy.Build(new[] { v1 });

        Assert.Equal(1, hierarchy.Count);
        Assert.Null(hierarchy.Parent(v1));
        Assert.Equal(1, hierarchy.DepthOf(v1));
        Assert.Equal("/v1", hierarchy.FullPrefixOf(v1));
    }

    [Fact]
    public void Build_MultipleIndependentHierarchies_HandlesCorrectly()
    {
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
        var api1 = Group(compilation, "Api1Group");
        var v1 = Group(compilation, "V1Group");
        var api2 = Group(compilation, "Api2Group");
        var v2 = Group(compilation, "V2Group");

        var hierarchy = GroupHierarchy.Build(new[] { api1, v1, api2, v2 });

        Assert.Equal(4, hierarchy.Count);
        Assert.Null(hierarchy.Parent(api1));
        Assert.Null(hierarchy.Parent(api2));
        Assert.Equal("Api1Group", hierarchy.Parent(v1)!.ClassType.FullName);
        Assert.Equal("Api2Group", hierarchy.Parent(v2)!.ClassType.FullName);
        Assert.Empty(hierarchy.Cycles);
    }

    [Fact]
    public void Build_FedSameGroupTwice_DoesNotThrow()
    {
        // Defense in depth: discovery de-dups, but the hierarchy must not throw on a duplicate FQN.
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api"")]
public class ApiGroup { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var api1 = Group(compilation, "ApiGroup");
        var api2 = Group(compilation, "ApiGroup");

        var hierarchy = GroupHierarchy.Build(new[] { api1, api2 });

        Assert.Equal(1, hierarchy.Count);
        Assert.Empty(hierarchy.Cycles);
    }

    #endregion
}
