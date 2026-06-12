# MinimalEndpoints Samples

This directory contains sample projects demonstrating various features of MinimalEndpoints.

## Available Samples

### 1. MinimalEndpoints.Sample
Basic example showing fundamental features:
- Simple GET and POST endpoints (`/test/{id}`, `/test-with-dependency`, `/v2/test-with-dependency`, POST `/test`)
- Dependency injection
- Parameter binding (route parameter `{id}`)
- Basic configuration

### 2. MinimalEndpoints.AdvancedSample
Advanced features demonstration:
- `IConfigurableEndpoint` implementation
- Hierarchical groups with `[MapGroup]` and `ParentGroup`
- Conditional mapping via `IConditionallyMapped`
- Route constraints and parameters (`{id:int}`)
- Constructor dependency injection and `ILogger`
- OpenAPI (Scalar API reference UI)
- Manual request validation
- Output caching

## Running the Samples

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 / VS Code / JetBrains Rider

### Basic Sample
```bash
cd samples/MinimalEndpoints.Sample
dotnet run
```

Then navigate to: `https://localhost:7207/scalar` (Scalar API reference; HTTP profile: `http://localhost:5160/scalar`)

### Advanced Sample
```bash
cd samples/MinimalEndpoints.AdvancedSample
dotnet run
```

Then navigate to: `https://localhost:7207/scalar` (Scalar API reference; HTTP profile: `http://localhost:5160/scalar`)

## Learning Path

1. Start with **MinimalEndpoints.Sample** to understand the basics
2. Explore **MinimalEndpoints.AdvancedSample** for advanced patterns

## Support

For questions or issues:
- 📚 [Documentation](../docs/)
- 💬 [Discussions](https://github.com/smavrommatis/MinimalEndpoints/discussions)
- 🐛 [Issues](https://github.com/smavrommatis/MinimalEndpoints/issues)

