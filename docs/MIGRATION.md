# Migration Guide

This guide helps you migrate to MinimalEndpoints from various approaches.

## Table of Contents

1. [From Traditional Minimal APIs](#from-traditional-minimal-apis)
2. [From MVC Controllers](#from-mvc-controllers)
3. [From FastEndpoints](#from-fastendpoints)
4. [From Carter](#from-carter)
5. [Breaking Changes Between Versions](#breaking-changes)

---

## From Traditional Minimal APIs

### Before: Traditional Minimal API

```csharp
// Program.cs - becomes cluttered quickly
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();

// All endpoints in one file
app.MapGet("/api/users", async (IUserRepository repo) =>
{
    var users = await repo.GetAllAsync();
    return Results.Ok(users);
});

app.MapGet("/api/users/{id}", async (int id, IUserRepository repo) =>
{
    var user = await repo.GetByIdAsync(id);
    return user != null ? Results.Ok(user) : Results.NotFound();
});

app.MapPost("/api/users", async (CreateUserRequest request, IUserRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest("Name is required");

    var user = await repo.CreateAsync(request);
    return Results.Created($"/api/users/{user.Id}", user);
});

// ... many more endpoints making this file huge
app.Run();
```

### After: MinimalEndpoints

```csharp
// Program.cs - stays clean!
using MinimalEndpoints.Generated;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddMinimalEndpoints(); // One line to register all endpoints

var app = builder.Build();

app.UseMinimalEndpoints(); // One line to map all endpoints

app.Run();
```

```csharp
// Endpoints/Users/GetUsersEndpoint.cs
[MapGet("/api/users")]
public class GetUsersEndpoint
{
    private readonly IUserRepository _repository;

    public GetUsersEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync()
    {
        var users = await _repository.GetAllAsync();
        return Results.Ok(users);
    }
}
```

```csharp
// Endpoints/Users/GetUserEndpoint.cs
[MapGet("/api/users/{id}")]
public class GetUserEndpoint
{
    private readonly IUserRepository _repository;

    public GetUserEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }
}
```

```csharp
// Endpoints/Users/CreateUserEndpoint.cs
[MapPost("/api/users")]
public class CreateUserEndpoint
{
    private readonly IUserRepository _repository;

    public CreateUserEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Name is required");

        var user = await _repository.CreateAsync(request);
        return Results.Created($"/api/users/{user.Id}", user);
    }
}
```

### Migration Steps

1. **Install Package**
   ```bash
   dotnet add package Blackeye.MinimalEndpoints
   ```

2. **Move Each Endpoint to a Class**
   - Create a class for each endpoint
   - Add appropriate `[MapGet]`, `[MapPost]`, etc. attribute
   - Move lambda body to `HandleAsync` method
   - Constructor inject dependencies instead of parameter injection

3. **Update Program.cs**
   ```csharp
   // Replace all app.MapGet/Post/etc calls with:
   builder.Services.AddMinimalEndpoints();
   app.UseMinimalEndpoints();
   ```

4. **Organize Files**
   ```
   Endpoints/
   ├── Users/
   │   ├── GetUsersEndpoint.cs
   │   ├── GetUserEndpoint.cs
   │   └── CreateUserEndpoint.cs
   └── Products/
       └── ...
   ```

### Benefits After Migration

- ✅ **Organized Code** - Each endpoint in its own file
- ✅ **Better Testing** - Easy to unit test individual endpoints
- ✅ **Constructor Injection** - Cleaner dependency management
- ✅ **Compile-Time Safety** - Analyzers catch errors early
- ✅ **Clean Program.cs** - No endpoint clutter
- ✅ **Same Performance** - Zero runtime overhead

---

## From MVC Controllers

### Before: MVC Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserRepository repository, ILogger<UsersController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        var users = await _repository.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(CreateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _repository.CreateAsync(request);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
}
```

### After: MinimalEndpoints

```csharp
// Endpoints/Users/GetUsersEndpoint.cs
[MapGet("/api/users")]
public class GetUsersEndpoint
{
    private readonly IUserRepository _repository;

    public GetUsersEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync()
    {
        var users = await _repository.GetAllAsync();
        return Results.Ok(users);
    }
}

// Endpoints/Users/GetUserEndpoint.cs
[MapGet("/api/users/{id}")]
public class GetUserEndpoint
{
    private readonly IUserRepository _repository;

    public GetUserEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }
}

// Endpoints/Users/CreateUserEndpoint.cs
[MapPost("/api/users")]
public class CreateUserEndpoint
{
    private readonly IUserRepository _repository;

    public CreateUserEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync([FromBody] CreateUserRequest request)
    {
        // Manual validation or use FluentValidation
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Name is required");

        var user = await _repository.CreateAsync(request);
        return Results.Created($"/api/users/{user.Id}", user);
    }
}
```

### Key Differences

| Feature | MVC Controller | MinimalEndpoints |
|---------|---------------|------------------|
| Return Type | `ActionResult<T>` | `IResult` or `Task<IResult>` |
| Base Class | `ControllerBase` | None (POCO) |
| Routing | `[Route]` + `[Http*]` | `[MapGet]`, `[MapPost]`, etc. |
| Organization | Multiple actions per controller | One endpoint per class |
| Model Validation | Automatic via `[ApiController]` | Manual or FluentValidation |
| Response Helpers | `Ok()`, `NotFound()` | `Results.Ok()`, `Results.NotFound()` |

### Migration Steps

1. **Install Package**
   ```bash
   dotnet add package Blackeye.MinimalEndpoints
   ```

2. **Update Program.cs**
   ```csharp
   // Remove:
   // builder.Services.AddControllers();
   // app.MapControllers();

   // Add:
   builder.Services.AddMinimalEndpoints();
   app.UseMinimalEndpoints();
   ```

3. **Convert Each Action to Endpoint**
   - Create new endpoint class for each controller action
   - Change `ActionResult<T>` to `IResult`
   - Change `Ok()` to `Results.Ok()`
   - Add `[MapGet("/route")]` attribute
   - Rename method to `HandleAsync`

4. **Handle Validation**
   ```csharp
   // Option 1: Manual validation
   if (string.IsNullOrWhiteSpace(request.Name))
       return Results.BadRequest("Name is required");

   // Option 2: FluentValidation
   var validator = new CreateUserRequestValidator();
   var validation = await validator.ValidateAsync(request);
   if (!validation.IsValid)
       return Results.ValidationProblem(validation.ToDictionary());
   ```

### What You Gain

- ✅ **Simpler Model** - No base classes needed
- ✅ **Better Organization** - One class per endpoint
- ✅ **Smaller Memory Footprint** - No controller overhead
- ✅ **Same Features** - All ASP.NET Core features still available
- ✅ **Faster Startup** - No controller discovery

### What Changes

- ⚠️ **No Automatic Model Validation** - Must validate manually or use FluentValidation
- ⚠️ **Different Return Types** - Use `IResult` instead of `ActionResult<T>`
- ⚠️ **No [ApiController] Magic** - More explicit, but more control

---

## From FastEndpoints

### Before: FastEndpoints

```csharp
public class GetUsersEndpoint : EndpointWithoutRequest<List<User>>
{
    public IUserRepository Repository { get; set; }

    public override void Configure()
    {
        Get("/api/users");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var users = await Repository.GetAllAsync();
        await SendAsync(users, cancellation: ct);
    }
}
```

### After: MinimalEndpoints

```csharp
[MapGet("/api/users")]
public class GetUsersEndpoint
{
    private readonly IUserRepository _repository;

    public GetUsersEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync()
    {
        var users = await _repository.GetAllAsync();
        return Results.Ok(users);
    }
}
```

### Key Differences

| Feature | FastEndpoints | MinimalEndpoints |
|---------|--------------|------------------|
| Approach | Runtime reflection | Compile-time generation |
| Performance | Small overhead | Zero overhead |
| Base Class | `Endpoint<TRequest, TResponse>` | None (POCO) |
| Configuration | `Configure()` method | `IConfigurableEndpoint` interface |
| DI | Property injection | Constructor injection |
| Validation | Built-in | Manual or FluentValidation |

### Migration Steps

1. **Remove FastEndpoints, Add MinimalEndpoints**
   ```bash
   dotnet remove package FastEndpoints
   dotnet add package Blackeye.MinimalEndpoints
   ```

2. **Update Program.cs**
   ```csharp
   // Remove:
   // builder.Services.AddFastEndpoints();
   // app.UseFastEndpoints();

   // Add:
   builder.Services.AddMinimalEndpoints();
   app.UseMinimalEndpoints();
   ```

3. **Convert Each Endpoint**
   - Remove base class inheritance
   - Move `Configure()` to `IConfigurableEndpoint` if needed
   - Change property injection to constructor injection
   - Replace `SendAsync()` with `return Results.Ok()`
   - Add `[MapGet]` attribute

### Advanced Configuration

```csharp
// FastEndpoints style
public override void Configure()
{
    Get("/api/users");
    AllowAnonymous();
    Description(x => x.WithTags("Users"));
}

// MinimalEndpoints equivalent
[MapGet("/api/users")]
public class GetUsersEndpoint : IConfigurableEndpoint
{
    public IResult Handle() => Results.Ok();

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint.AllowAnonymous()
                .WithTags("Users");
    }
}
```

---

## From Carter

### Before: Carter

```csharp
public class UsersModule : CarterModule
{
    public UsersModule() : base("/api/users")
    {
        this.IncludeInOpenApi();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (IUserRepository repo) =>
        {
            var users = await repo.GetAllAsync();
            return Results.Ok(users);
        });

        app.MapGet("/{id}", async (int id, IUserRepository repo) =>
        {
            var user = await repo.GetByIdAsync(id);
            return user != null ? Results.Ok(user) : Results.NotFound();
        });
    }
}
```

### After: MinimalEndpoints with Groups

```csharp
// Groups/UsersGroup.cs
[MapGroup("/api/users")]
public class UsersGroup : IConfigurableGroup  // Optional: for configuration
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithOpenApi();
    }
}

// Endpoints/GetUsersEndpoint.cs
[MapGet("/", Group = typeof(UsersGroup))]
public class GetUsersEndpoint
{
    private readonly IUserRepository _repository;

    public GetUsersEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync()
    {
        var users = await _repository.GetAllAsync();
        return Results.Ok(users);
    }
}

// Endpoints/GetUserEndpoint.cs
[MapGet("/{id}", Group = typeof(UsersGroup))]
public class GetUserEndpoint
{
    private readonly IUserRepository _repository;

    public GetUserEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }
}
```

### Migration Steps

1. **Replace Packages**
   ```bash
   dotnet remove package Carter
   dotnet add package Blackeye.MinimalEndpoints
   ```

2. **Update Program.cs**
   ```csharp
   // Remove:
   // builder.Services.AddCarter();
   // app.MapCarter();

   // Add:
   builder.Services.AddMinimalEndpoints();
   app.UseMinimalEndpoints();
   ```

3. **Convert Modules to Groups**
   - Create groups with `[MapGroup]` attribute for each `CarterModule`
   - Optionally implement `IConfigurableGroup` for configuration
   - Move each `MapGet/Post` to separate endpoint class
   - Reference group in endpoint: `Group = typeof(UsersGroup)`

---

## Breaking Changes

### Version 1.0.x → Future Versions

Currently on stable release (1.0.0). Future breaking changes will be documented here.

**Anticipated Changes:**
- Future versions may require .NET 9.0+ for new features

### Handling Breaking Changes

Always check [CHANGELOG.md](../../CHANGELOG.md) before upgrading.

```bash
# Check what changed
git diff v1.0.0 v2.0.0 CHANGELOG.md

# Upgrade gradually
dotnet add package Blackeye.MinimalEndpoints --version 2.0.0

# Run tests
dotnet test
```

---

## Migration Checklist

- [ ] Install MinimalEndpoints package
- [ ] Update Program.cs with `AddMinimalEndpoints()` and `UseMinimalEndpoints()`
- [ ] Create endpoint classes with appropriate attributes
- [ ] Move logic from old approach to `HandleAsync()` methods
- [ ] Update dependency injection to constructor injection
- [ ] Create groups for related endpoints
- [ ] Update tests
- [ ] Remove old packages/code
- [ ] Run full test suite
- [ ] Update documentation

---

## Getting Help

- 📚 [Documentation](README.md)
- 💬 [Discussions](https://github.com/smavrommatis/MinimalEndpoints/discussions)
- 🐛 [Issues](https://github.com/smavrommatis/MinimalEndpoints/issues)

---

**Having trouble migrating?** [Ask for help in Discussions](https://github.com/smavrommatis/MinimalEndpoints/discussions/new?category=q-a)

