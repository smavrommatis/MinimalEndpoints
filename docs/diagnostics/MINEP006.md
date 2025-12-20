# MINEP006: Cyclic Group Hierarchy Detected

## Diagnostic ID

`MINEP006`

## Severity

Error

## Version History

- **v1.0.0 (2025-12-20)**: Initial implementation. Detects cyclic dependencies in group hierarchies using the `ParentGroup` property.

## Description

The group '{0}' has a cyclic hierarchy. Group hierarchies must form a proper tree structure without cycles.

## Message

> Group '{0}' has a cyclic hierarchy: {1}. Group hierarchies must not contain cycles.

## Cause

This error occurs when:
1. A group directly or indirectly references itself as a parent
2. A circular chain of parent relationships exists between groups
3. The group hierarchy forms a cycle instead of a tree

## How to Fix

### Remove the Cyclic Dependency

Reorganize your group hierarchy to form a proper tree structure.

**❌ Incorrect - Direct Cycle:**
```csharp
[MapGroup("/api", ParentGroup = typeof(ApiGroup))]  // ❌ References itself!
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}
```

**✅ Correct:**
```csharp
[MapGroup("/api")]  // ✅ No parent - root of hierarchy
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}
```

---

**❌ Incorrect - Two-Level Cycle:**
```csharp
[MapGroup("/api", ParentGroup = typeof(V1Group))]
public class ApiGroup : IEndpointGroup { }

[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]  // ❌ Cycle: Api -> V1 -> Api
public class V1Group : IEndpointGroup { }
```

**✅ Correct:**
```csharp
[MapGroup("/api")]  // Root
public class ApiGroup : IEndpointGroup { }

[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]  // ✅ Child of Api
public class V1Group : IEndpointGroup { }
```

---

**❌ Incorrect - Three-Level Cycle:**
```csharp
[MapGroup("/api", ParentGroup = typeof(V2Group))]  // ❌ Cycle!
public class ApiGroup : IEndpointGroup { }

[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup { }

[MapGroup("/v2", ParentGroup = typeof(V1Group))]  // Api -> V2 -> V1 -> Api
public class V2Group : IEndpointGroup { }
```

**✅ Correct - Proper Hierarchy:**
```csharp
[MapGroup("/api")]  // Root
public class ApiGroup : IEndpointGroup { }

[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]  // Level 1
public class V1Group : IEndpointGroup { }

[MapGroup("/products", ParentGroup = typeof(V1Group))]  // Level 2
public class ProductsGroup : IEndpointGroup { }

// Results in hierarchy:
// ApiGroup (/)
//   └─ V1Group (/api/v1)
//       └─ ProductsGroup (/api/v1/products)
```

## Examples

### Valid Group Hierarchies

**Example 1: Simple Two-Level Hierarchy**
```csharp
[MapGroup("/api")]
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithOpenApi();
    }
}

[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization();
    }
}

[MapGet("/products", Group = typeof(V1Group))]
public class ListProductsEndpoint { }
// Results in: /api/v1/products
```

**Example 2: Multi-Level Hierarchy**
```csharp
[MapGroup("/api")]
public class ApiGroup : IEndpointGroup { }

[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup { }

[MapGroup("/admin", ParentGroup = typeof(V1Group))]
public class AdminGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization("Admin");
    }
}

[MapGet("/users", Group = typeof(AdminGroup))]
public class ListUsersEndpoint { }
// Results in: /api/v1/admin/users
```

**Example 3: Multiple Branches**
```csharp
[MapGroup("/api")]
public class ApiGroup : IEndpointGroup { }

// Branch 1: V1
[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup { }

[MapGroup("/products", ParentGroup = typeof(V1Group))]
public class V1ProductsGroup : IEndpointGroup { }

// Branch 2: V2
[MapGroup("/v2", ParentGroup = typeof(ApiGroup))]
public class V2Group : IEndpointGroup { }

[MapGroup("/products", ParentGroup = typeof(V2Group))]
public class V2ProductsGroup : IEndpointGroup { }

// Results in tree:
// ApiGroup (/api)
//   ├─ V1Group (/api/v1)
//   │   └─ V1ProductsGroup (/api/v1/products)
//   └─ V2Group (/api/v2)
//       └─ V2ProductsGroup (/api/v2/products)
```

## Benefits of Group Hierarchies

### 1. Nested Configuration
```csharp
[MapGroup("/api")]
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithOpenApi();  // All descendants get OpenAPI
    }
}

[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization();  // All V1 endpoints require auth
    }
}

[MapGroup("/admin", ParentGroup = typeof(V1Group))]
public class AdminGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization("Admin");  // Admin-specific auth
    }
}
```

### 2. API Versioning
```csharp
[MapGroup("/api")]
public class ApiGroup : IEndpointGroup { }

[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithTags("V1");
    }
}

[MapGroup("/v2", ParentGroup = typeof(ApiGroup))]
public class V2Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithTags("V2")
             .WithRateLimiter("strict");
    }
}
```

### 3. Feature Grouping
```csharp
[MapGroup("/api")]
public class ApiGroup : IEndpointGroup { }

[MapGroup("/products", ParentGroup = typeof(ApiGroup))]
public class ProductsGroup : IEndpointGroup { }

[MapGroup("/orders", ParentGroup = typeof(ApiGroup))]
public class OrdersGroup : IEndpointGroup { }

[MapGroup("/users", ParentGroup = typeof(ApiGroup))]
public class UsersGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization();
    }
}
```

## Relationship with Other Diagnostics

### Works With MINEP004 (Ambiguous Routes)
```csharp
[MapGroup("/api")]
public class ApiGroup : IEndpointGroup { }

[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup { }

[MapGet("/products", Group = typeof(V1Group))]  // /api/v1/products
public class GetProductsV1 { }

[MapGet("/api/v1/products")]  // Also /api/v1/products
public class GetProductsDirect { }
// ⚠️ MINEP004: Ambiguous routes detected!
```

### Works With MINEP005 (Invalid Group Type)
```csharp
[MapGroup("/api", ParentGroup = typeof(InvalidGroup))]  // ❌ MINEP005 if InvalidGroup is invalid
public class ApiGroup : IEndpointGroup { }
```

## Debugging Tips

### Visualize Your Hierarchy
When you have a complex group structure, draw it out:

```
Root Groups (no parent)
│
├─ ApiGroup (/api)
│   ├─ V1Group (/api/v1)
│   │   ├─ ProductsGroup (/api/v1/products)
│   │   └─ OrdersGroup (/api/v1/orders)
│   └─ V2Group (/api/v2)
│       └─ ProductsGroup (/api/v2/products)
│
└─ PublicGroup (/public)
    └─ HealthGroup (/public/health)
```

### Check for Indirect Cycles
The cycle might not be obvious:
```csharp
A -> B -> C -> D -> B  // Cycle involves B, C, D
```

## See Also

- [MINEP004: Ambiguous Routes](MINEP004.md) - Route conflict detection
- [MINEP005: Invalid Group Type](MINEP005.md) - Group validation
- [Documentation: Hierarchical Groups](../README.md#hierarchical-groups)

## Technical Details

The analyzer uses depth-first search (DFS) to detect cycles in the group hierarchy graph. It tracks the current path and marks visited nodes to efficiently find any cycles.

**Algorithm:**
1. For each group, start DFS from that group
2. Track current path (groups being explored)
3. If we visit a group already in the current path → cycle detected
4. If we visit a group already fully explored → no cycle from there
5. Report all detected cycles

This ensures all cyclic dependencies are caught at compile-time before code generation.

