using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
    private readonly List<string> _sources = new();
    private readonly List<MetadataReference> _references = new();
    private readonly HashSet<string> _addedAssemblies = new(StringComparer.OrdinalIgnoreCase);
    private bool _hasMvcReferences;
    private bool _hasComponentModelReferences;
    private readonly string _assemblyName;

    public CompilationBuilder(string source, string assemblyName = "TestAssembly")
    {
        _assemblyName = assemblyName;
        _sources.Add(source);

        // Add minimal required references
        AddReferenceIfNotExists(typeof(object).Assembly); // System.Private.CoreLib
        AddReferenceIfNotExists(typeof(Console).Assembly); // System.Console
        AddReferenceIfNotExists(typeof(Enumerable).Assembly); // System.Linq
        AddReferenceIfNotExists(typeof(IConfigurableEndpoint).Assembly);
        WithComponentModelReferences();
        WithSystemAssemblies();
    }

    /// <summary>
    /// Adds an additional source file, producing a second syntax tree in the compilation.
    /// Use this to exercise scenarios that span multiple files.
    /// </summary>
    public CompilationBuilder WithAdditionalSource(string source)
    {
        _sources.Add(source);
        return this;
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
    /// Emits <paramref name="referenced"/> to an in-memory PE image and adds it as a metadata
    /// reference. This reproduces a real "referenced compiled assembly": the resulting compilation
    /// sees only metadata symbols with NO syntax trees — exactly the condition under which
    /// <c>ForAttributeWithMetadataName</c> cannot discover the referenced endpoints/groups. A
    /// <see cref="CompilationReference"/> would keep the source compilation (and its syntax) attached
    /// and would NOT reproduce that condition, so it is deliberately not used here.
    /// </summary>
    public CompilationBuilder WithReferencedAssembly(CSharpCompilation referenced)
    {
        using var peStream = new MemoryStream();
        var emitResult = referenced.Emit(peStream);

        if (!emitResult.Success)
        {
            var errors = emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(e => $"{e.Id}: {e.GetMessage()}");
            throw new InvalidOperationException(
                $"Referenced compilation failed to emit:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }

        _references.Add(MetadataReference.CreateFromImage(peStream.ToArray()));
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
    /// Adds references for the broad set of System.* (and netstandard) platform assemblies.
    /// </summary>
    /// <remarks>
    /// Resolved deterministically from TRUSTED_PLATFORM_ASSEMBLIES — the fixed set of platform
    /// assemblies the host runtime was launched with — rather than AppDomain.CurrentDomain
    /// .GetAssemblies(), whose contents depend on what has been loaded/JITted so far (test execution
    /// order, the runner) and could non-deterministically include or miss a System.* type. The TPA
    /// set is a superset of the loaded set, so this only adds references, never removes them.
    /// </remarks>
    private CompilationBuilder WithSystemAssemblies()
    {
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string trustedPlatformAssemblies ||
            trustedPlatformAssemblies.Length == 0)
        {
            return this;
        }

        foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
        {
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            var name = Path.GetFileNameWithoutExtension(path);
            if (name == "System.Runtime" ||
                name == "netstandard" ||
                name.StartsWith("System.", StringComparison.Ordinal))
            {
                AddReferenceIfNotExists(path);
            }
        }

        return this;
    }

    /// <summary>
    /// Builds the CSharpCompilation and validates it for errors.
    /// </summary>
    public CSharpCompilation Build(bool validateCompilation = true)
    {
        var syntaxTrees = _sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToList();

        var usings = new List<string>
        {
            "System",
            "System.Threading",
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
        syntaxTrees.Add(globalUsingsSyntaxTree);

        var compilation = CSharpCompilation.Create(
            _assemblyName,
            syntaxTrees,
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

        AddReferenceIfNotExists(assembly.Location);
    }

    private void AddReferenceIfNotExists(string location)
    {
        if (string.IsNullOrEmpty(location))
        {
            return;
        }

        if (_addedAssemblies.Add(location))
        {
            _references.Add(MetadataReference.CreateFromFile(location));
        }
    }
}
