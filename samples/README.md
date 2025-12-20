# MinimalEndpoints Samples

This directory contains sample projects demonstrating various features of MinimalEndpoints.

## Available Samples

### 1. MinimalEndpoints.Sample
Basic example showing fundamental features:
- Simple GET/POST/PUT/DELETE endpoints
- Dependency injection
- Parameter binding
- Basic configuration

### 2. MinimalEndpoints.AdvancedSample
Advanced features demonstration:
- `IConfigurableEndpoint` implementation
- `ServiceType` with interfaces
- Route constraints and parameters
- Authentication and authorization
- OpenAPI/Swagger integration
- Validation
- File uploads

### 3. MinimalEndpoints.RealWorldSample
Production-ready example application:
- Complete CRUD operations
- Database integration (EF Core)
- Authentication with JWT
- Authorization policies
- Error handling
- Logging
- Health checks
- Docker support

## Running the Samples

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 / VS Code / JetBrains Rider

### Basic Sample
```bash
cd samples/MinimalEndpoints.Sample
dotnet run
```

Then navigate to: `https://localhost:5001/swagger`

### Advanced Sample
```bash
cd samples/MinimalEndpoints.AdvancedSample
dotnet run
```

### Real-World Sample
```bash
cd samples/MinimalEndpoints.RealWorldSample
dotnet run
```

Or with Docker:
```bash
cd samples/MinimalEndpoints.RealWorldSample
docker-compose up
```

## Learning Path

1. Start with **MinimalEndpoints.Sample** to understand the basics
2. Explore **MinimalEndpoints.AdvancedSample** for advanced patterns
3. Study **MinimalEndpoints.RealWorldSample** for production best practices

## Support

For questions or issues:
- 📚 [Documentation](../docs/)
- 💬 [Discussions](https://github.com/yourusername/MinimalEndpoints/discussions)
- 🐛 [Issues](https://github.com/yourusername/MinimalEndpoints/issues)

