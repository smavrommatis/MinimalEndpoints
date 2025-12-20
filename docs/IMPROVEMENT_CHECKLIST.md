# MinimalEndpoints Improvement Checklist

## Priority 1: Critical (Must Have) ✅ COMPLETED

### Tests

- [x] **Add test for IConfigurableEndpoint with ServiceType integration** ✅
  - Verify that when an endpoint implements `IConfigurableEndpoint` AND has `ServiceType` set, the `Configure` method is called correctly
  - Ensure the `Configure` method uses the concrete class type, not the interface type
  - Test case: `GeneratedCode_HandlesServiceTypeWithConfigurableEndpoint_Correctly`

- [x] **Add Analyzer: ServiceType must contain EntryPoint method** ✅
  - Create new diagnostic `MINEP003`: "ServiceType interface must declare the entry point method"
  - Verify that if `ServiceType` is specified, the interface contains the same entry point method signature
  - Add analyzer test in new file: `MinimalEndpointsAnalyzerTests.cs`
  - Test cases:
    - `ServiceType_WithMatchingEntryPoint_NoError`
    - `ServiceType_WithMissingEntryPoint_ReportsError`
    - `ServiceType_WithDifferentSignature_ReportsError`

- [x] **Add Analyzer Diagnostic Tests** ✅
  - Create `MinimalEndpointsAnalyzerTests.cs` for testing analyzer diagnostics
  - Test `MINEP001` (MissingEntryPoint):
    - `MissingEntryPoint_WithNoHandleMethod_ReportsError`
    - `MissingEntryPoint_WithCustomEntryPointNotFound_ReportsError`
    - `MissingEntryPoint_WithCorrectMethod_NoError`
  - Test `MINEP002` (MultipleAttributes):
    - `MultipleAttributes_WithTwoMapAttributes_ReportsError`
    - `MultipleAttributes_WithSingleAttribute_NoError`
  - Test `MINEP003` (ServiceType validation):
    - `ServiceTypeValidation_WithMatchingInterface_NoError`
    - `ServiceTypeValidation_WithNonMatchingInterface_ReportsError`

- [x] **Test naming generation correctness** ✅
  - Add tests in `EndToEndCodeGenerationTests.cs`:
    - `GeneratedMethodName_ForNestedClass_UsesCorrectFormat` - Test `Namespace.Outer+Inner` becomes `Map__Namespace_Outer_Inner`
    - `GeneratedMethodName_ForGenericClass_HandlesGenerics` - Test generic classes (e.g., `Handler<T>`)
    - `GeneratedMethodName_ForLongNamespace_DoesNotExceedLimits` - Test very long namespaces
    - `GeneratedMethodName_ForSpecialCharacters_EscapesCorrectly` - Test special characters in names
    - `GeneratedMethodName_IsUnique_ForDifferentEndpoints` - Ensure no naming collisions

### XML Documentation

- [x] **Add XML documentation to all public APIs** ✅
  - `IConfigurableEndpoint` interface - Describe purpose and usage
  - All attributes in `Annotations` folder:
    - `MapGetAttribute`, `MapPostAttribute`, `MapPutAttribute`, `MapDeleteAttribute`, `MapPatchAttribute`, `MapHeadAttribute`, `MapMethodsAttribute`
    - Document all properties: `Pattern`, `Lifetime`, `GroupPrefix`, `EntryPoint`, `ServiceType`
  - Document generated extension methods in code generator comments
  - Add `<example>` tags with code samples for each attribute

---

## Priority 2: High (Should Have)

### Performance Improvements

- [ ] **Optimize string concatenation in EndpointCodeGenerator**
  - Replace string concatenation in loops with `StringBuilder` or `StringPool`
  - Target: `BuildParameterList`, `BuildParameterAttributes` methods
  - Expected improvement: Reduce allocations in generated code

- [ ] **Cache TypeDefinition display strings**
  - Add memoization for `TypeDefinition.ToDisplayString()` results
  - Avoid repeated calls to `SymbolDisplayFormat` with same usings
  - Store computed display strings in `TypeDefinition` after first calculation

- [ ] **Use ArrayBuilder instead of List<T> in hot paths**
  - Replace `List<string>` with `ArrayBuilder<string>` or `ImmutableArray.Builder<T>` in:
    - `BuildParameterList` method
    - `CSharpFileScope` collections
  - Reduces allocations during code generation

- [ ] **Optimize attribute lookup in analyzer**
  - Cache `WellKnownTypes` symbols per compilation
  - Use `SymbolEqualityComparer` for faster attribute comparison
  - Target: `GetMapMethodAttributes` and `IsMapMethodsAttribute` methods

- [ ] **Use span-based string operations**
  - Replace `string.Replace` chains in `MappingEndpointMethodName` with `Span<char>` operations
  - Use `MemoryExtensions.Replace` for better performance

- [ ] **Incremental generator optimizations**
  - Add `ForAttributeWithMetadataName` instead of manual filtering in `CreateSyntaxProvider`
  - Cache endpoint definitions using `IEqualityComparer<EndpointDefinition>`
  - Reduce incremental generation triggers

### Tests

- [ ] **Add integration test: ServiceType with IConfigurableEndpoint**
  - Test case: `GeneratedCode_HandlesServiceTypeWithConfigurableEndpoint_Correctly`
  - Verify both interface registration and Configure call on concrete type

- [ ] **Add test for parameter collision edge cases**
  - Test case: `GeneratedCode_HandlesMultipleParameterCollisions_WithEndpointInstanceAndApp`
  - Test renaming when both `endpointInstance` and `app` parameters exist

- [ ] **Add test for complex generic scenarios**
  - Test case: `GeneratedCode_HandlesNestedGenerics_Correctly` - `Task<Result<Option<T>>>`
  - Test case: `GeneratedCode_HandlesTupleTypes_Correctly` - `(int, string)` tuple parameters
  - Test case: `GeneratedCode_HandlesNullableReferenceTypes_Correctly` - `string?` vs `string`

- [ ] **Add test for different attribute combinations**
  - Test case: `GeneratedCode_HandlesAllAttributeTypes_OnSameParameter`
  - Test case: `GeneratedCode_HandlesAttributeInheritance_Correctly`

- [ ] **Add test for edge cases in entry point detection**
  - Test case: `FindEntryPoint_WithOverloadedMethods_SelectsCorrectOne`
  - Test case: `FindEntryPoint_WithShadowedMethods_SelectsDeclaredMethod`

---

## Priority 3: Medium (Nice to Have)

### Code Quality & Readability

- [ ] **Refactor CSharpFileScope for better testability**
  - Split `CSharpFileScope.Build()` into smaller methods
  - Extract `BuildUsingsSection()`, `BuildClassDeclaration()`, `BuildMethods()`
  - Make internal state more observable for testing

- [ ] **Simplify EndpointDefinition.Create()**
  - Extract parameter mapping logic to separate method
  - Add factory methods for testing: `CreateForTesting()` with defaults

- [ ] **Add builder pattern for test helpers**
  - Create `EndpointDefinitionBuilder` for tests
  - Create `CompilationBuilder` improvements:
    - `WithGlobalUsing()` method
    - `WithPreprocessorDirective()` method

- [ ] **Improve error messages in diagnostics**
  - Add more context to `MINEP001` error - suggest method names
  - Add code fix suggestions to diagnostic messages
  - Include example code in error descriptions

### Tests

- [ ] **Add test for different lifetime combinations**
  - Test case: `GeneratedCode_HandlesAllLifetimeCombinations_InSingleFile`
  - Verify service registration for Singleton, Scoped, Transient in same assembly

- [ ] **Add test for special route patterns**
  - Test case: `GeneratedCode_HandlesRouteConstraints_Correctly` - `/users/{id:int}`
  - Test case: `GeneratedCode_HandlesOptionalParameters_InRoute` - `/users/{id?}`
  - Test case: `GeneratedCode_HandlesCatchAllParameters_Correctly` - `/{**catch-all}`

- [ ] **Add test for namespace edge cases**
  - Test case: `GeneratedCode_HandlesGlobalNamespace_Correctly`
  - Test case: `GeneratedCode_HandlesFileScoped Namespaces_Correctly`

- [ ] **Add benchmark tests**
  - Create `EndpointGeneratorBenchmarks.cs` using BenchmarkDotNet
  - Benchmark code generation speed for 100, 1000, 10000 endpoints
  - Benchmark analyzer performance on large solutions

### Documentation

- [ ] **Add code examples to EXAMPLES.md**
  - ServiceType with IConfigurableEndpoint example
  - Complex generic parameter example
  - Middleware integration example
  - Authentication/Authorization example
  - OpenAPI/Swagger integration example

- [ ] **Create ARCHITECTURE.md**
  - Document the source generator pipeline
  - Explain analyzer architecture
  - Show the relationship between models
  - Include diagrams (optional)

- [ ] **Enhance README.md**
  - Add "How It Works" section
  - Add "Performance" section with metrics
  - Add "Migration Guide" from controller-based APIs
  - Add "Troubleshooting" section

---

## Priority 4: Low (Could Have)

### Advanced Features

- [ ] **Add Analyzer: Detect unused endpoints**
  - Create diagnostic `MINEP004`: "Endpoint is registered but never mapped"
  - Warn if endpoint is in `AddMinimalEndpoints()` but not in `UseMinimalEndpoints()`

- [ ] **Add Analyzer: Detect ambiguous routes**
  - Create diagnostic `MINEP005`: "Route pattern conflicts with another endpoint"
  - Check for duplicate patterns across endpoints

- [ ] **Add Code Fix: Generate IConfigurableEndpoint.Configure method**
  - When class is marked with attribute, offer to implement `IConfigurableEndpoint`
  - Auto-generate stub `Configure` method

- [ ] **Add Code Fix: Generate missing entry point**
  - When `MINEP001` is triggered, offer to generate `HandleAsync` method
  - Include proper return type and parameters based on route

### Tests

- [ ] **Add test for concurrent endpoint registration**
  - Test thread-safety of generated code
  - Verify concurrent calls to `AddMinimalEndpoints()`

- [ ] **Add test for dependency resolution**
  - Test case: `GeneratedCode_ResolvesTransitiveDependencies_Correctly`
  - Verify complex DI scenarios work

- [ ] **Add integration tests with real ASP.NET Core app**
  - Create `MinimalEndpoints.IntegrationTests` project
  - Test actual HTTP requests to generated endpoints
  - Test middleware pipeline integration

- [ ] **Add test for analyzer performance**
  - Test case: `Analyzer_PerformsWell_OnLargeSolution`
  - Measure time to analyze 10,000 classes

### Performance

- [ ] **Investigate AOT compatibility**
  - Test with NativeAOT compilation
  - Ensure no reflection is used in generated code
  - Add warning if incompatible patterns detected

- [ ] **Add source caching between builds**
  - Cache generated sources when inputs haven't changed
  - Investigate using incremental generator's caching more effectively

### Documentation

- [ ] **Create video tutorial**
  - Getting started guide
  - Advanced features walkthrough
  - Migration from controllers

- [ ] **Add interactive examples**
  - Create sample project with all features
  - Add to `samples/` directory
  - Include Docker setup for quick testing

---

## Additional Suggestions

### Code Fixes to Implement

- [ ] **Code Fix for MINEP002 (Multiple attributes)**
  - Offer to remove duplicate attributes
  - Show preview of which attribute will be kept

- [ ] **Code Fix for missing using directives**
  - When attribute is not found, offer to add `using MinimalEndpoints.Annotations;`

### Extensibility

- [ ] **Support custom attributes**
  - Allow users to create custom mapping attributes
  - Document how to extend the analyzer

- [ ] **Support endpoint filters**
  - Generate code for `IEndpointFilter` implementations
  - Allow filters to be specified via attributes

### Testing Infrastructure

- [ ] **Improve CompilationBuilder**
  - Add support for multi-target framework testing
  - Add support for analyzer configuration (editorconfig)
  - Add method to capture diagnostics without throwing

- [ ] **Create test utilities package**
  - Extract `CompilationBuilder` to separate testing library
  - Allow other projects to test their source generators more easily

---

## Summary

**Total Items**: 62
- Priority 1 (Critical): 13 items
- Priority 2 (High): 15 items
- Priority 3 (Medium): 14 items
- Priority 4 (Low): 20 items

**Recommended Next Steps**:
1. Start with Priority 1 items (analyzer for ServiceType, missing tests)
2. Add XML documentation to all public APIs
3. Implement performance improvements from Priority 2
4. Add comprehensive test coverage for edge cases
5. Enhance documentation with real-world examples

