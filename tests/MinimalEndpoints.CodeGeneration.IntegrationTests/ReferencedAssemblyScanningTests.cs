using Microsoft.CodeAnalysis.CSharp;

namespace MinimalEndpoints.CodeGeneration.IntegrationTests;

/// <summary>
/// Covers cross-assembly discovery: endpoints/groups defined in a *referenced compiled assembly*
/// (a real PE metadata reference with no syntax trees) are only registered when the host opts in
/// with <c>[assembly: ScanReferencedEndpoints]</c>. The default path must never see them.
/// </summary>
public class ReferencedAssemblyScanningTests
{
    // A library with a public group and a public endpoint mapped into that group. Compiled to a PE
    // image and referenced by the host, so it reaches the generator only as metadata symbols.
    private const string LibrarySource = @"
namespace Ext.Library;

[MapGroup(""/ext"")]
public class ExtGroup
{
}

[MapGet(""/widget"", Group = typeof(ExtGroup))]
public class ExtEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok(""widget""));
}";

    // The host always has its own local endpoint, so the generator emits output regardless of whether
    // the referenced endpoints are discovered — making the "not discovered" assertion unambiguous.
    private const string HostEndpointSource = @"
namespace Host.App;

[MapGet(""/host"")]
public class HostEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok(""host""));
}";

    private const string OptInAttribute = "[assembly: MinimalEndpoints.Annotations.ScanReferencedEndpoints]\n";

    private static CSharpCompilation BuildLibrary() =>
        new CompilationBuilder(LibrarySource).WithMvcReferences().Build();

    [Fact]
    public void EndpointInReferencedAssembly_NotDiscovered_WhenNotOptedIn()
    {
        var host = new CompilationBuilder(HostEndpointSource)
            .WithMvcReferences()
            .WithReferencedAssembly(BuildLibrary())
            .Build(validateCompilation: false);

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(host);

        // The host's own endpoint is registered...
        Assert.NotNull(generatedCode);
        Assert.Contains("Map__Host_App_HostEndpoint", generatedCode);
        // ...but the referenced library's endpoint and group are NOT.
        Assert.DoesNotContain("ExtEndpoint", generatedCode);
        Assert.DoesNotContain("ExtGroup", generatedCode);
    }

    [Fact]
    public void ReferencedEndpointAndGroup_Discovered_WhenOptedIn()
    {
        var host = new CompilationBuilder(OptInAttribute + HostEndpointSource)
            .WithMvcReferences()
            .WithReferencedAssembly(BuildLibrary())
            .Build(validateCompilation: false);

        // validateCompilation:true also proves the generated code BINDS against the referenced
        // assembly's public types across the boundary, not just that the right text was emitted.
        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(host, validateCompilation: true);

        Assert.NotNull(generatedCode);
        Assert.Contains("Map__Host_App_HostEndpoint", generatedCode);
        Assert.Contains("Map__Ext_Library_ExtEndpoint", generatedCode);
        Assert.Contains("MapGroup__Ext_Library_ExtGroup", generatedCode);
        // The referenced endpoint is mapped INTO the referenced group — cross-assembly composition.
        Assert.Contains("builder.Map__Ext_Library_ExtEndpoint(app, group_Ext_Library_ExtGroup);", generatedCode);
    }

    [Fact]
    public void ReferencedDefinitions_DeterministicAcrossReferenceOrder()
    {
        var libAlpha = new CompilationBuilder(@"
namespace Ext.A;

[MapGet(""/a"")]
public class AlphaEndpoint
{
    public IResult Handle() => Results.Ok(""a"");
}", assemblyName: "Ext.Alpha").WithMvcReferences().Build();

        var libBeta = new CompilationBuilder(@"
namespace Ext.B;

[MapGet(""/b"")]
public class BetaEndpoint
{
    public IResult Handle() => Results.Ok(""b"");
}", assemblyName: "Ext.Beta").WithMvcReferences().Build();

        var hostAlphaBeta = new CompilationBuilder(OptInAttribute + HostEndpointSource)
            .WithMvcReferences().WithReferencedAssembly(libAlpha).WithReferencedAssembly(libBeta)
            .Build(validateCompilation: false);

        var hostBetaAlpha = new CompilationBuilder(OptInAttribute + HostEndpointSource)
            .WithMvcReferences().WithReferencedAssembly(libBeta).WithReferencedAssembly(libAlpha)
            .Build(validateCompilation: false);

        var (genAlphaBeta, _) = CompilationUtilities.GenerateCodeAndCompile(hostAlphaBeta, validateCompilation: false);
        var (genBetaAlpha, _) = CompilationUtilities.GenerateCodeAndCompile(hostBetaAlpha, validateCompilation: false);

        // Both referenced endpoints must actually be discovered — otherwise two empty scans would also
        // compare equal and this test would pass without exercising the sort at all.
        Assert.Contains("Map__Ext_A_AlphaEndpoint", genAlphaBeta);
        Assert.Contains("Map__Ext_B_BetaEndpoint", genAlphaBeta);

        // The scan sorts referenced definitions by FQN, so emission is identical regardless of the
        // order the references were added in.
        Assert.Equal(genAlphaBeta, genBetaAlpha);
    }

    [Fact]
    public void ConfigurableAndConditionalReferencedTypes_EmitTheirStaticHooks()
    {
        var lib = new CompilationBuilder(@"
namespace Ext.Cfg;

[MapGroup(""/cfg"")]
public class CfgGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) { }
}

[MapGet(""/c"", Group = typeof(CfgGroup))]
public class CfgEndpoint : IConfigurableEndpoint, IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint) { }
    public IResult Handle() => Results.Ok();
}").WithMvcReferences().Build();

        var host = new CompilationBuilder(OptInAttribute + HostEndpointSource)
            .WithMvcReferences().WithReferencedAssembly(lib).Build(validateCompilation: false);

        // validateCompilation:true proves the cross-assembly STATIC hook calls bind against the
        // referenced public types — the strongest proof configurable/conditional types work across
        // the assembly boundary (interface detection via AllInterfaces works on metadata symbols).
        var (gen, _) = CompilationUtilities.GenerateCodeAndCompile(host, validateCompilation: true);

        Assert.Contains(".ConfigureGroup(app, group);", gen);
        Assert.Contains(".ShouldMap(app)", gen);
        Assert.Contains(".Configure(app, endpoint);", gen);
    }

    [Fact]
    public void InternalReferencedEndpoint_IsSilentlySkipped()
    {
        var lib = new CompilationBuilder(@"
namespace Ext.Vis;

[MapGet(""/public"")]
public class VisibleEndpoint
{
    public IResult Handle() => Results.Ok();
}

[MapGet(""/hidden"")]
internal class HiddenEndpoint
{
    public IResult Handle() => Results.Ok();
}").WithMvcReferences().Build();

        var host = new CompilationBuilder(OptInAttribute + HostEndpointSource)
            .WithMvcReferences().WithReferencedAssembly(lib).Build(validateCompilation: false);

        var (gen, _) = CompilationUtilities.GenerateCodeAndCompile(host, validateCompilation: true);

        // The public referenced endpoint is registered; the internal one is not referenceable across
        // the assembly boundary, so it is skipped (no diagnostic, consistent with the same-assembly gate).
        Assert.Contains("VisibleEndpoint", gen);
        Assert.DoesNotContain("HiddenEndpoint", gen);
    }

    [Fact]
    public void DuplicateFqn_SourceAndReferenced_RegisteredOnce()
    {
        const string duplicate = @"
namespace Dup;

[MapGet(""/dup"")]
public class Thing
{
    public IResult Handle() => Results.Ok();
}";
        var lib = new CompilationBuilder(duplicate).WithMvcReferences().Build();

        var host = new CompilationBuilder(OptInAttribute + duplicate)
            .WithMvcReferences().WithReferencedAssembly(lib).Build(validateCompilation: false);

        var (gen, _) = CompilationUtilities.GenerateCodeAndCompile(host, validateCompilation: false);

        // The source definition wins the FQN de-dup, so exactly one Map method is DECLARED (failing to
        // de-dup would emit two and produce CS0111). The declaration uniquely contains "(this".
        Assert.Equal(1, CountOccurrences(gen, "Map__Dup_Thing(this"));
    }

    [Fact]
    public void OptedIn_WithReferenceThatHasNoEndpoints_EmitsNothingExtra_NoThrow()
    {
        var lib = new CompilationBuilder(@"
namespace Ext.Empty;

public class JustAClass
{
}").WithMvcReferences().Build();

        var host = new CompilationBuilder(OptInAttribute + HostEndpointSource)
            .WithMvcReferences().WithReferencedAssembly(lib).Build(validateCompilation: false);

        var (gen, _) = CompilationUtilities.GenerateCodeAndCompile(host, validateCompilation: true);

        // Opting in with a referenced library that has no endpoints must add nothing: the output equals
        // what the host produces with no reference at all (and generation does not throw).
        var baseline = new CompilationBuilder(OptInAttribute + HostEndpointSource)
            .WithMvcReferences().Build(validateCompilation: false);
        var (genBaseline, _) = CompilationUtilities.GenerateCodeAndCompile(baseline, validateCompilation: true);

        Assert.Contains("Map__Host_App_HostEndpoint", gen);
        Assert.Equal(genBaseline, gen);
    }

    [Fact]
    public void OptIn_TargetingOneAssembly_ScansOnlyThatAssembly()
    {
        var libAlpha = new CompilationBuilder(@"
namespace Ext.A;

[MapGet(""/a"")]
public class AlphaEndpoint
{
    public IResult Handle() => Results.Ok();
}", assemblyName: "Ext.Alpha").WithMvcReferences().Build();

        var libBeta = new CompilationBuilder(@"
namespace Ext.B;

[MapGet(""/b"")]
public class BetaEndpoint
{
    public IResult Handle() => Results.Ok();
}", assemblyName: "Ext.Beta").WithMvcReferences().Build();

        // Opt in but TARGET ONLY Ext.Alpha (via a marker type in it); Ext.Beta must be ignored.
        var host = new CompilationBuilder(
                "[assembly: MinimalEndpoints.Annotations.ScanReferencedEndpoints(typeof(Ext.A.AlphaEndpoint))]\n"
                + HostEndpointSource)
            .WithMvcReferences()
            .WithReferencedAssembly(libAlpha)
            .WithReferencedAssembly(libBeta)
            .Build(validateCompilation: false);

        var (gen, _) = CompilationUtilities.GenerateCodeAndCompile(host, validateCompilation: true);

        Assert.Contains("Map__Ext_A_AlphaEndpoint", gen);   // targeted assembly → scanned
        Assert.DoesNotContain("BetaEndpoint", gen);          // non-targeted assembly → skipped
    }

    [Fact]
    public void OptedIn_ReferencedEndpoint_NonPublicServiceType_RegistersConcrete()
    {
        var lib = new CompilationBuilder(@"
namespace Ext.Svc;

internal interface IHiddenService
{
    IResult Handle();
}

[MapGet(""/svc"", ServiceType = typeof(IHiddenService))]
public class SvcEndpoint : IHiddenService
{
    public IResult Handle() => Results.Ok();
}", assemblyName: "Ext.Svc").WithMvcReferences().Build();

        var host = new CompilationBuilder(OptInAttribute + HostEndpointSource)
            .WithMvcReferences().WithReferencedAssembly(lib).Build(validateCompilation: false);

        // validateCompilation:true proves the generated code compiles — the internal ServiceType is
        // dropped and the endpoint is registered as the concrete public class instead of emitting CS0122.
        var (gen, _) = CompilationUtilities.GenerateCodeAndCompile(host, validateCompilation: true);

        Assert.Contains("Map__Ext_Svc_SvcEndpoint", gen);
        Assert.DoesNotContain("IHiddenService", gen);
    }

    [Fact]
    public void OptedIn_ReferencedEndpoint_NonPublicParameterAttribute_IsSkipped()
    {
        var lib = new CompilationBuilder(@"
namespace Ext.Attr;

internal class HiddenAttribute : System.Attribute { }

[MapGet(""/attr"")]
public class AttrEndpoint
{
    public IResult Handle([Hidden] int x) => Results.Ok();
}", assemblyName: "Ext.Attr").WithMvcReferences().Build();

        var host = new CompilationBuilder(OptInAttribute + HostEndpointSource)
            .WithMvcReferences().WithReferencedAssembly(lib).Build(validateCompilation: false);

        var (gen, _) = CompilationUtilities.GenerateCodeAndCompile(host, validateCompilation: true);

        // The handler parameter carries an internal attribute the host can't name, so the endpoint is
        // skipped rather than emitting [Ext.Attr.Hidden] (which would be CS0122). The host endpoint stays.
        Assert.DoesNotContain("AttrEndpoint", gen);
        Assert.Contains("Map__Host_App_HostEndpoint", gen);
    }

    private static int CountOccurrences(string text, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(token, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }
}
