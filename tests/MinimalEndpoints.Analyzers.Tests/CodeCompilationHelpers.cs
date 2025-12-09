using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MinimalEndpoints.Analyzers.Tests;

public static class CodeCompilationHelpers
{
    public static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Builder.IApplicationBuilder).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Builder.IEndpointConventionBuilder).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(MinimalEndpoints.IConfigurableEndpoint).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.ServiceLifetime).Assembly.Location),
        };

        // Add all loaded assemblies from Current Domain
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            {
                if (assembly.GetName().Name == "System.Runtime" ||
                    assembly.GetName().Name == "netstandard" ||
                    assembly.GetName().Name?.StartsWith("System.") == true)
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }
        }
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Validate compilation for errors
        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

        if (errors.Length > 0)
        {
            var errorMessages = string.Join(Environment.NewLine,
                errors.Select(e => $"{e.Id}: {e.GetMessage()}"));
            throw new InvalidOperationException(
                $"Compilation failed with {errors.Length} error(s):{Environment.NewLine}{errorMessages}");
        }

        return compilation;
    }
}


