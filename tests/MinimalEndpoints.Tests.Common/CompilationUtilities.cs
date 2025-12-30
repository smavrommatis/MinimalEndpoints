using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using MinimalEndpoints.CodeGeneration;
using MinimalEndpoints.CodeGeneration.Endpoints.Analyzers;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;
using MinimalEndpoints.CodeGeneration.Groups;
using MinimalEndpoints.CodeGeneration.Groups.Analyzers;
using MinimalEndpoints.CodeGeneration.Groups.Models;
using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.Tests.Common;

public static class CompilationUtilities
{
    public static List<Diagnostic> GenerateDiagnostics(CSharpCompilation compilation)
    {
        var minimalEndpointsAnalyzer = new EndpointsAnalyzer();
        var ambiguousRouteAnalyzer = new GroupsAnalyzer();
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
            minimalEndpointsAnalyzer,
            ambiguousRouteAnalyzer);

        var compilationWithAnalyzer = compilation.WithAnalyzers(analyzers);

        var diagnostics = compilationWithAnalyzer.GetAllDiagnosticsAsync().Result;

        return diagnostics
            .Where(d => d.Id.StartsWith("MINEP"))
            .ToList();
    }

    public static (string generatedCode, IEnumerable<Diagnostic> diagnostics) GenerateCodeAndCompile(
        CSharpCompilation compilation,
        bool validateCompilation = true
    )
    {
        // Get all endpoint classes
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var definitions = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Select(classDecl => semanticModel.GetDeclaredSymbol(classDecl))
            .Where(symbol => symbol != null)
            .Select(SymbolDefinitionFactory.TryCreateSymbol)
            .Where(ed => ed != null)
            .ToImmutableArray();

        if (definitions.IsEmpty)
        {
            return (null!, Enumerable.Empty<Diagnostic>());
        }

        var endpoints = new List<EndpointDefinition>(definitions.Length);
        var groupDefinitions = new List<EndpointGroupDefinition>(definitions.Length);
        Dictionary<INamedTypeSymbol, EndpointGroupDefinition> groups;

        foreach (var def in definitions)
        {
            switch (def)
            {
                case EndpointDefinition endpoint:
                    endpoints.Add(endpoint);
                    break;
                case EndpointGroupDefinition group:
                    groupDefinitions.Add(group);
                    break;
            }
        }

        groups = groupDefinitions.Count > 0
            ? groupDefinitions.FillHierarchyAndDetectCycles()
            : new Dictionary<INamedTypeSymbol, EndpointGroupDefinition>(SymbolEqualityComparer.Default);

        // Generate code
        var fileScope = MinimalEndpointsFileBuilder.GenerateFile(
            "MinimalEndpoints.Generated",
            "MinimalEndpointExtensions",
            endpoints,
            groups
        );

        var generatedCode = fileScope?.Build();

        // Compile generated code with original to verify it compiles
        if (generatedCode != null)
        {
            var generatedTree = CSharpSyntaxTree.ParseText(generatedCode);
            var newCompilation = compilation.AddSyntaxTrees(generatedTree);
            var diagnostics = newCompilation.GetDiagnostics();

            if (validateCompilation)
            {
                var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

                if (errors.Length > 0)
                {
                    var errorMessages = string.Join(Environment.NewLine,
                        errors.Select(e => $"{e.Id}: {e.GetMessage()}"));
                    throw new InvalidOperationException(
                        $"Compilation failed with {errors.Length} error(s):{Environment.NewLine}{errorMessages}");
                }
            }

            return (generatedCode, diagnostics);
        }

        return (null!, compilation.GetDiagnostics());
    }
}
