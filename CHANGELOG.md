# Changelog

All notable changes to MinimalEndpoints will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2025-12-21

### Added
- **Production Release**: First stable release of MinimalEndpoints
- Class-based endpoint organization for ASP.NET Core Minimal APIs
- Source generator for zero runtime overhead code generation
- Six diagnostic analyzers (MINEP001-MINEP006):
  - MINEP001: Missing entry point method detection
  - MINEP002: Multiple MapMethods attributes validation
  - MINEP003: ServiceType interface validation
  - MINEP004: Ambiguous route detection
  - MINEP005: Invalid endpoint group validation
  - MINEP006: Cyclic group hierarchy detection
- Code fix providers for automatic issue resolution
- Support for all HTTP methods (GET, POST, PUT, DELETE, PATCH, HEAD)
- Endpoint groups with hierarchical organization
- `IEndpointGroup` for shared endpoint configuration
- `IConfigurableEndpoint` for advanced endpoint setup
- Full dependency injection support (constructor and parameter injection)
- Service lifetime control (Singleton, Scoped, Transient)
- Service interface registration support
- Custom entry point method names
- Comprehensive documentation and examples
- Basic and advanced sample projects
- Performance benchmarks suite
- Complete test coverage
- ASP.NET Core integration examples (API Versioning, Caching, Rate Limiting, OpenTelemetry, Authorization)
- Migration guides from Minimal APIs, MVC, FastEndpoints, and Carter
- Troubleshooting guide with 50+ solutions
- Performance optimization guide
- Comparison documentation with alternatives

### Technical Details
- Target Framework: .NET 10.0
- Language Version: C# 14.0
- Source Generator: Incremental generator with syntax filtering
- Analyzers: Concurrent execution enabled
- Code Generation: Compile-time with no runtime overhead

### Dependencies
- Microsoft.CodeAnalysis.CSharp 5.0.0 (analyzer/generator)
- Microsoft.AspNetCore.App (runtime)

### Known Limitations
- Groups must not form circular hierarchies
- Only one MapMethods attribute per endpoint class
- Entry point method must be public and non-static


---

## Release Notes

### Version 1.0.0 - First Stable Release

This is the first production-ready stable release of MinimalEndpoints, bringing class-based organization to ASP.NET Core Minimal APIs with zero runtime overhead.

**Highlights:**
- 🎯 Clean, class-based endpoint organization
- ⚡ Zero runtime overhead through source generation
- 🔧 Six compile-time analyzers with helpful diagnostics
- 💉 Full dependency injection support
- 🏗️ Hierarchical endpoint groups
- 📊 Comprehensive test coverage
- 📚 Extensive documentation and examples

**Getting Started:**
```bash
dotnet add package Blackeye.MinimalEndpoints
```

See [README.md](README.md) for quick start guide and [docs/](docs/) for detailed documentation.

**Reporting Issues:**
Please report bugs and feature requests at https://github.com/smavrommatis/MinimalEndpoints/issues

**Contributing:**
See [CONTRIBUTING.md](docs/CONTRIBUTING.md) for contribution guidelines.

