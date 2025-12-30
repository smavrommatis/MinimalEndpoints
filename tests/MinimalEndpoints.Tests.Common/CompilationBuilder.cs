using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Tests.Common;

/// <summary>
/// Builder for creating CSharpCompilation instances with a fluent API.
/// </summary>
public class CompilationBuilder
{
    private readonly string _source;
    private readonly List<MetadataReference> _references = new();
    private readonly HashSet<string> _addedAssemblies = new(StringComparer.OrdinalIgnoreCase);
    private bool _hasMvcReferences;
    private bool _hasComponentModelReferences;

    public CompilationBuilder(string source)
    {
        _source = source;

        // Add minimal required references
        AddReferenceIfNotExists(typeof(object).Assembly); // System.Private.CoreLib
        AddReferenceIfNotExists(typeof(Console).Assembly); // System.Console
        AddReferenceIfNotExists(typeof(Enumerable).Assembly); // System.Linq
        AddReferenceIfNotExists(typeof(IConfigurableEndpoint).Assembly);
        WithComponentModelReferences();
        WithSystemAssemblies();
    }

    /// <summary>
    /// Adds a reference to the assembly containing the specified type.
    /// </summary>
    public CompilationBuilder WithTypeAssembly(Type type)
    {
        AddReferenceIfNotExists(type.Assembly);
        return this;
    }

    /// <summary>
    /// Adds references for ASP.NET Core MVC, DependencyInjection, and MinimalEndpoints.
    /// </summary>
    public CompilationBuilder WithMvcReferences()
    {
        _hasMvcReferences = true;
        AddReferenceIfNotExists(typeof(IApplicationBuilder).Assembly);
        AddReferenceIfNotExists(typeof(IEndpointRouteBuilder).Assembly);
        AddReferenceIfNotExists(typeof(FromBodyAttribute).Assembly);
        AddReferenceIfNotExists(typeof(ServiceLifetime).Assembly);
        AddReferenceIfNotExists(typeof(Results).Assembly);
        AddReferenceIfNotExists(typeof(RouteData).Assembly);

        return this;
    }

    /// <summary>
    /// Adds references for System.ComponentModel (Description, DefaultValue, etc.).
    /// </summary>
    private CompilationBuilder WithComponentModelReferences()
    {
        _hasComponentModelReferences = true;
        AddReferenceIfNotExists(typeof(DescriptionAttribute).Assembly);
        AddReferenceIfNotExists(typeof(RequiredAttribute).Assembly);
        return this;
    }

    /// <summary>
    /// Adds references for all System.* assemblies from the current AppDomain.
    /// Use this when you need broad System library support.
    /// </summary>
    private CompilationBuilder WithSystemAssemblies()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            {
                var name = assembly.GetName().Name;
                if (name == "System.Runtime" ||
                    name == "netstandard" ||
                    name?.StartsWith("System.") == true)
                {
                    AddReferenceIfNotExists(assembly);
                }
            }
        }

        return this;
    }

    /// <summary>
    /// Builds the CSharpCompilation and validates it for errors.
    /// </summary>
    public CSharpCompilation Build(bool validateCompilation = true)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(_source);

        var usings = new List<string>
        {
            "System",
            "System.Threading.Tasks",
            "System.Collections.Generic",
            "System.Linq",
            "MinimalEndpoints",
            "MinimalEndpoints.Annotations"
        };

        if (_hasMvcReferences)
        {
            usings.Add("Microsoft.AspNetCore.Builder");
            usings.Add("Microsoft.AspNetCore.Routing");
            usings.Add("Microsoft.AspNetCore.Mvc");
            usings.Add("Microsoft.Extensions.DependencyInjection");
            usings.Add("Microsoft.AspNetCore.Http");
        }

        if (_hasComponentModelReferences)
        {
            usings.Add("System.ComponentModel");
            usings.Add("System.ComponentModel.DataAnnotations");
        }

        var globalUsingsSyntaxTree = CSharpSyntaxTree.ParseText(string.Join(Environment.NewLine, usings.Select(u => $"global using {u};")));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree, globalUsingsSyntaxTree },
            _references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                usings: usings
            )
        );

        if (validateCompilation)
        {
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
        }

        return compilation;
    }

    private void AddReferenceIfNotExists(Assembly assembly)
    {
        if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
        {
            return;
        }

        var location = assembly.Location;
        if (_addedAssemblies.Add(location))
        {
            _references.Add(MetadataReference.CreateFromFile(location));
        }
    }
}
