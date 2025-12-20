# Security Policy

## Supported Versions

We release patches for security vulnerabilities for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

## Reporting a Vulnerability

The MinimalEndpoints team takes security bugs seriously. We appreciate your efforts to responsibly disclose your findings.

### How to Report a Security Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report them via email to:

**sotirios.mavrommatis+security@gmail.com**

You should receive a response within 48 hours. If for some reason you do not, please follow up via email to ensure we received your original message.

### What to Include in Your Report

Please include the following information along with your report:

- Type of issue (e.g., buffer overflow, SQL injection, cross-site scripting, etc.)
- Full paths of source file(s) related to the manifestation of the issue
- The location of the affected source code (tag/branch/commit or direct URL)
- Any special configuration required to reproduce the issue
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit it

This information will help us triage your report more quickly.

### Disclosure Policy

When we receive a security bug report, we will:

1. Confirm the problem and determine the affected versions
2. Audit code to find any similar problems
3. Prepare fixes for all supported releases
4. Release new security patch versions as quickly as possible

### Comments on This Policy

If you have suggestions on how this process could be improved, please submit a pull request or email us at sotirios.mavrommatis+security@gmail.com.

## Security Best Practices for Users

When using MinimalEndpoints, we recommend following these security best practices:

### 1. Input Validation

Always validate input parameters in your endpoints:

```csharp
[MapPost("/users")]
public class CreateUserEndpoint
{
    public IResult Handle([FromBody] CreateUserRequest request)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest("Email is required");

        if (!IsValidEmail(request.Email))
            return Results.BadRequest("Invalid email format");

        // Process request...
    }
}
```

### 2. Authorization

Always protect sensitive endpoints with authorization:

```csharp
[MapGet("/admin/users")]
public class GetAdminUsersEndpoint : IConfigurableEndpoint
{
    public IResult Handle() => Results.Ok();

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint.RequireAuthorization("AdminPolicy");
    }
}
```

### 3. Rate Limiting

Implement rate limiting to prevent abuse:

```csharp
[MapPost("/api/orders")]
public class CreateOrderEndpoint : IConfigurableEndpoint
{
    public IResult Handle() => Results.Ok();

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint.RequireRateLimiting("fixed");
    }
}
```

### 4. Sensitive Data

Never expose sensitive data in responses or logs:

```csharp
[MapGet("/users/{id}")]
public class GetUserEndpoint
{
    private readonly ILogger<GetUserEndpoint> _logger;

    public async Task<IResult> HandleAsync(int id)
    {
        var user = await GetUser(id);

        // DON'T log sensitive data
        // _logger.LogInformation("User: {@User}", user); // Bad!

        // DO log non-sensitive identifiers
        _logger.LogInformation("Retrieved user {UserId}", id); // Good!

        // Return DTO without sensitive fields
        return Results.Ok(user.ToPublicDto());
    }
}
```

### 5. CORS Configuration

Configure CORS carefully for production:

```csharp
// In Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production",
        policy => policy
            .WithOrigins("https://yourdomain.com") // Specific origins only
            .AllowedHeaders("Content-Type", "Authorization")
            .AllowedMethods("GET", "POST")
            .AllowCredentials());
});
```

### 6. HTTPS Enforcement

Always use HTTPS in production:

```csharp
// In Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
```

### 7. SQL Injection Prevention

Use parameterized queries or ORMs:

```csharp
[MapGet("/users/{search}")]
public class SearchUsersEndpoint
{
    private readonly DbContext _db;

    public async Task<IResult> HandleAsync(string search)
    {
        // Good: Parameterized query
        var users = await _db.Users
            .Where(u => u.Name.Contains(search))
            .ToListAsync();

        // BAD: String concatenation
        // var sql = $"SELECT * FROM Users WHERE Name LIKE '%{search}%'"; // NEVER DO THIS

        return Results.Ok(users);
    }
}
```

### 8. Mass Assignment Protection

Use DTOs to prevent mass assignment vulnerabilities:

```csharp
// Good: Use specific DTOs
public record CreateUserRequest(string Name, string Email); // Only allowed fields

[MapPost("/users")]
public class CreateUserEndpoint
{
    public IResult Handle([FromBody] CreateUserRequest request)
    {
        // User can only set Name and Email, not IsAdmin or other sensitive fields
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            IsAdmin = false // Controlled by server
        };
        // ...
    }
}
```

## Security Considerations for Contributors

If you're contributing to MinimalEndpoints:

1. **Analyzer Security**: Ensure analyzers handle malicious code gracefully without crashes
2. **Code Generation**: Generated code must not introduce vulnerabilities
3. **Dependency Updates**: Keep analyzer dependencies up to date
4. **Input Validation**: Validate all attribute parameters in analyzers
5. **Safe Defaults**: Design APIs with secure defaults (e.g., Scoped lifetime, not Singleton)

## Security Advisories

Security advisories will be published at:
https://github.com/smavrommatis/MinimalEndpoints/security/advisories

## Hall of Fame

We recognize and thank security researchers who have helped improve MinimalEndpoints:

<!-- Security researchers will be listed here after responsible disclosure -->

---

**Last Updated:** December 21, 2025

