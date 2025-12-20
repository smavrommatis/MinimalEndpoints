# Contributing to MinimalEndpoints

First off, thank you for considering contributing to MinimalEndpoints! It's people like you that make MinimalEndpoints such a great tool.

## Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [How Can I Contribute?](#how-can-i-contribute)
3. [Development Setup](#development-setup)
4. [Pull Request Process](#pull-request-process)
5. [Coding Guidelines](#coding-guidelines)
6. [Testing Guidelines](#testing-guidelines)
7. [Documentation Guidelines](#documentation-guidelines)

---

## Code of Conduct

This project and everyone participating in it is governed by our Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior via [GitHub Issues](https://github.com/smavrommatis/MinimalEndpoints/issues) or email to [sotirios.mavrommatis+conduct@gmail.com](mailto:sotirios.mavrommatis+conduct@gmail.com).

### Our Standards

- Using welcoming and inclusive language
- Being respectful of differing viewpoints
- Gracefully accepting constructive criticism
- Focusing on what is best for the community
- Showing empathy towards other community members

---

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the issue list as you might find out that you don't need to create one. When you are creating a bug report, please include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples** - Include links to files or copy/pasteable snippets
- **Describe the behavior you observed** and point out what exactly is the problem
- **Explain which behavior you expected** to see instead and why
- **Include screenshots** if possible
- **Include your environment details**: OS, .NET version, IDE version

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, please include:

- **Use a clear and descriptive title**
- **Provide a step-by-step description** of the suggested enhancement
- **Provide specific examples** to demonstrate the steps
- **Describe the current behavior** and explain the behavior you expected to see
- **Explain why this enhancement would be useful**

### Your First Code Contribution

Unsure where to begin? You can start by looking through `good-first-issue` and `help-wanted` issues:

- **good-first-issue** - Issues which should only require a few lines of code
- **help-wanted** - Issues which should be a bit more involved than beginner issues

---

## Development Setup

### Prerequisites

- .NET 10.0 SDK or later
- Visual Studio 2022 / VS Code / Rider
- Git

### Clone and Build

```bash
# Clone the repository
git clone https://github.com/blackeye/MinimalEndpoints.git
cd MinimalEndpoints

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

### Project Structure

```
MinimalEndpoints/
├── src/
│   ├── MinimalEndpoints/              # Core library with attributes
│   ├── MinimalEndpoints.Analyzers/    # Roslyn analyzers and generators
│   └── MinimalEndpoints.CodeFixes/    # Code fix providers
├── tests/
│   └── MinimalEndpoints.Analyzers.Tests/  # Unit and integration tests
├── samples/
│   └── MinimalEndpoints.Sample/       # Sample application
├── docs/                              # Documentation
└── benchmarks/                        # Performance benchmarks
```

### Running the Sample

```bash
cd samples/MinimalEndpoints.Sample
dotnet run
```

Visit `https://localhost:5001` to see the sample API in action.

---

## Pull Request Process

1. **Fork the repository** and create your branch from `main`
2. **Make your changes** following our coding guidelines
3. **Add or update tests** for your changes
4. **Update documentation** if needed
5. **Ensure all tests pass**: `dotnet test`
6. **Ensure code compiles** without warnings: `dotnet build`
7. **Create a Pull Request** with a clear title and description

### PR Title Format

Use conventional commit format:

- `feat: Add new feature`
- `fix: Fix bug in code generation`
- `docs: Update README`
- `test: Add tests for TypeDefinition`
- `refactor: Simplify EndpointCodeGenerator`
- `perf: Optimize type name generation`
- `chore: Update dependencies`

### PR Description Template

```markdown
## Description
[Describe what this PR does]

## Motivation
[Why is this change needed?]

## Changes
- [List of changes]
- [Another change]

## Testing
[Describe how you tested these changes]

## Checklist
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] All tests passing
- [ ] No compiler warnings
- [ ] Follows coding guidelines
```

---

## Coding Guidelines

### General Principles

- **KISS** - Keep It Simple, Stupid
- **DRY** - Don't Repeat Yourself
- **YAGNI** - You Aren't Gonna Need It
- **Prefer composition over inheritance**
- **Write self-documenting code**

### C# Style

Follow the [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions):

```csharp
// ✅ DO: Use PascalCase for public members
public class EndpointDefinition
{
    public string Name { get; init; }
}

// ✅ DO: Use camelCase with underscore for private fields
private readonly ILogger _logger;

// ✅ DO: Use meaningful names
public void ProcessUserRequest() { }

// ❌ DON'T: Use abbreviations
public void ProcUsrReq() { }  // Bad

// ✅ DO: Add XML documentation for public APIs
/// <summary>
/// Generates code for the specified endpoint definition.
/// </summary>
/// <param name="endpoint">The endpoint to generate code for</param>
/// <returns>Generated C# code</returns>
public string GenerateCode(EndpointDefinition endpoint)

// ✅ DO: Use expression-bodied members when appropriate
public bool IsValid => Name != null && Name.Length > 0;

// ✅ DO: Use pattern matching
if (symbol is INamedTypeSymbol namedType)
{
    // Use namedType
}

// ✅ DO: Use init-only properties for immutable data
public record TypeDefinition
{
    public string Name { get; init; }
    public string Namespace { get; init; }
}

// ✅ DO: Use nullable reference types
public string? OptionalValue { get; set; }
```

### File Organization

```csharp
// 1. Using statements (sorted)
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

// 2. Namespace
namespace MinimalEndpoints.Analyzers.Models;

// 3. XML documentation
/// <summary>
/// Represents an endpoint definition.
/// </summary>

// 4. Class declaration
internal sealed class EndpointDefinition
{
    // 5. Constants
    private const int MaxLength = 100;

    // 6. Fields
    private readonly string _name;

    // 7. Constructors
    public EndpointDefinition(string name)
    {
        _name = name;
    }

    // 8. Properties
    public string Name => _name;

    // 9. Methods
    public void Process()
    {
        // Implementation
    }
}
```

### Performance Considerations

```csharp
// ✅ DO: Use StringBuilder for multiple concatenations
var sb = new StringBuilder();
foreach (var item in items)
{
    sb.Append(item);
}

// ❌ DON'T: Use string concatenation in loops
string result = "";
foreach (var item in items)
{
    result += item;  // Creates new string each iteration
}

// ✅ DO: Use collection initializers capacity when known
var list = new List<string>(capacity: 100);

// ✅ DO: Use ArrayPool for temporary arrays
var buffer = ArrayPool<byte>.Shared.Rent(size);
try
{
    // Use buffer
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}

// ✅ DO: Use span for string manipulation
ReadOnlySpan<char> span = text.AsSpan();
```

---

## Testing Guidelines

### Test Organization

```csharp
public class TypeDefinitionTests
{
    [Fact]
    public void Constructor_WithValidInput_CreatesInstance()
    {
        // Arrange
        var input = "test";

        // Act
        var result = new TypeDefinition(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(input, result.Name);
    }

    [Theory]
    [InlineData("System.String", "String")]
    [InlineData("System.Int32", "Int32")]
    public void GetSimpleName_ReturnsExpectedValue(string fullName, string expected)
    {
        // Arrange
        var typeDef = new TypeDefinition(fullName);

        // Act
        var result = typeDef.GetSimpleName();

        // Assert
        Assert.Equal(expected, result);
    }
}
```

### Test Naming

- **Use descriptive names**: `MethodName_Scenario_ExpectedBehavior`
- Examples:
  - `GenerateCode_WithNullInput_ThrowsArgumentNullException`
  - `ToDisplayString_WithUsings_SimplifiesTypeName`
  - `FindEntryPoint_WithMultipleMethods_PrefersAsync`

### Test Coverage

- **Aim for 80%+ code coverage**
- **Test edge cases** (null, empty, boundary values)
- **Test error conditions**
- **Test happy paths**

### Integration Tests

```csharp
[Fact]
public void EndToEnd_GeneratesValidCode()
{
    // Given a complete endpoint
    var source = @"
        [MapGet(""/test"")]
        public class TestEndpoint
        {
            public IResult Handle() => Results.Ok();
        }
    ";

    // When generating code
    var generated = GenerateCode(source);

    // Then the code should compile
    var compilation = Compile(source, generated);
    var diagnostics = compilation.GetDiagnostics();

    Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
}
```

---

## Documentation Guidelines

### XML Documentation

All public types and members should have XML documentation:

```csharp
/// <summary>
/// Generates endpoint mapping code from endpoint definitions.
/// </summary>
/// <remarks>
/// This class uses a fluent API to build C# code strings that register
/// and map ASP.NET Core minimal API endpoints.
/// </remarks>
public class EndpointCodeGenerator
{
    /// <summary>
    /// Generates registration and mapping code for the specified endpoints.
    /// </summary>
    /// <param name="namespaceName">The namespace for the generated class</param>
    /// <param name="className">The name of the generated class</param>
    /// <param name="endpoints">The endpoints to generate code for</param>
    /// <returns>
    /// A <see cref="CSharpFileScope"/> containing the generated code,
    /// or null if no endpoints are provided.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="namespaceName"/> or
    /// <paramref name="className"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// var fileScope = EndpointCodeGenerator.GenerateCode(
    ///     "MyApp.Generated",
    ///     "EndpointExtensions",
    ///     endpoints
    /// );
    /// </code>
    /// </example>
    public static CSharpFileScope GenerateCode(
        string namespaceName,
        string className,
        ImmutableArray<EndpointDefinition> endpoints)
    {
        // Implementation
    }
}
```

### README Updates

When adding new features:

1. Update the main README.md
2. Add examples to EXAMPLES.md
3. Update relevant documentation in docs/
4. Add changelog entry

### Code Comments

```csharp
// ✅ DO: Explain WHY, not WHAT
// Use StringBuilder to avoid allocating multiple strings in the loop
var sb = new StringBuilder();

// ❌ DON'T: State the obvious
// Create a StringBuilder
var sb = new StringBuilder();

// ✅ DO: Document complex algorithms
// Algorithm: Boyer-Moore string search
// Time complexity: O(n/m) best case, O(n*m) worst case
// ...

// ✅ DO: Mark technical debt
// TODO: Optimize this for large collections
// HACK: Workaround for compiler bug #12345
// NOTE: This assumes input is already validated
```

---

## Git Workflow

### Branching Strategy

- `main` - Stable release branch
- `develop` - Development branch
- `feature/feature-name` - Feature branches
- `fix/bug-description` - Bug fix branches
- `docs/documentation-update` - Documentation branches

### Commit Messages

Use conventional commits:

```
feat(analyzer): add support for generic endpoints

- Add TypeDefinition handling for generic types
- Update tests for generic scenarios
- Add documentation examples

Closes #123
```

Format: `type(scope): subject`

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only
- `style`: Formatting, missing semicolons, etc
- `refactor`: Code restructuring
- `perf`: Performance improvement
- `test`: Adding missing tests
- `chore`: Maintenance tasks

---

## Questions?

Don't hesitate to ask! You can:

- Open an issue with the `question` label
- Start a discussion on GitHub Discussions
- Reach out to the maintainers

---

**Thank you for contributing to MinimalEndpoints!** 🎉
