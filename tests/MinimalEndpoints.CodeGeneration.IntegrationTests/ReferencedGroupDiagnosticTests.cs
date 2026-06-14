using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MinimalEndpoints.CodeGeneration.IntegrationTests;

/// <summary>
/// MINEP009: a source endpoint/group references a <c>[MapGroup]</c> in a referenced assembly that the
/// host's cross-assembly scan won't cover, so the group is silently dropped. Fires when there is no
/// opt-in, or a targeted opt-in that excludes the group's assembly; does not fire when the assembly is
/// covered or the group is in the same compilation.
/// </summary>
public class ReferencedGroupDiagnosticTests
{
    private const string GroupLibrarySource = @"
namespace Ext;

[MapGroup(""/lib"")]
public class LibGroup { }";

    private const string EndpointUsingLibGroup = @"
namespace Host;

[MapGet(""/x"", Group = typeof(Ext.LibGroup))]
public class HostEndpoint
{
    public IResult Handle() => Results.Ok();
}";

    private static CSharpCompilation BuildGroupLibrary() =>
        new CompilationBuilder(GroupLibrarySource, assemblyName: "Ext.Lib").WithMvcReferences().Build();

    private static List<Diagnostic> DiagnosticsFor(string hostSource, params CSharpCompilation[] references)
    {
        var builder = new CompilationBuilder(hostSource).WithMvcReferences();
        foreach (var reference in references)
        {
            builder = builder.WithReferencedAssembly(reference);
        }

        return CompilationUtilities.GenerateDiagnostics(builder.Build(validateCompilation: false));
    }

    [Fact]
    public void EndpointReferencesCrossAssemblyGroup_NotOptedIn_ReportsMINEP009()
    {
        var diagnostics = DiagnosticsFor(EndpointUsingLibGroup, BuildGroupLibrary());

        var minep009 = Assert.Single(diagnostics, d => d.Id == "MINEP009");
        // The message names the referencing endpoint, the group, and its assembly — guards arg order.
        var message = minep009.GetMessage();
        Assert.Contains("HostEndpoint", message);
        Assert.Contains("LibGroup", message);
        Assert.Contains("Ext.Lib", message);
    }

    [Fact]
    public void EndpointReferencesCrossAssemblyGroup_OptedInScanAll_NoMINEP009()
    {
        var diagnostics = DiagnosticsFor(
            "[assembly: MinimalEndpoints.Annotations.ScanReferencedEndpoints]\n" + EndpointUsingLibGroup,
            BuildGroupLibrary());

        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP009");
    }

    [Fact]
    public void EndpointReferencesCrossAssemblyGroup_TargetedToThatAssembly_NoMINEP009()
    {
        var diagnostics = DiagnosticsFor(
            "[assembly: MinimalEndpoints.Annotations.ScanReferencedEndpoints(typeof(Ext.LibGroup))]\n" + EndpointUsingLibGroup,
            BuildGroupLibrary());

        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP009");
    }

    [Fact]
    public void EndpointReferencesCrossAssemblyGroup_TargetedToOtherAssembly_ReportsMINEP009()
    {
        // Opted in, but the targeted assembly is NOT the one defining the group → still not scanned.
        var other = new CompilationBuilder(@"
namespace Other;

[MapGet(""/o"")]
public class OtherEndpoint
{
    public IResult Handle() => Results.Ok();
}", assemblyName: "Other.Lib").WithMvcReferences().Build();

        var diagnostics = DiagnosticsFor(
            "[assembly: MinimalEndpoints.Annotations.ScanReferencedEndpoints(typeof(Other.OtherEndpoint))]\n" + EndpointUsingLibGroup,
            BuildGroupLibrary(),
            other);

        Assert.Contains(diagnostics, d => d.Id == "MINEP009");
    }

    [Fact]
    public void EndpointReferencesSameAssemblyGroup_NoMINEP009()
    {
        // Endpoint and group both in the current compilation — no cross-assembly concern.
        var code = @"
namespace Host;

[MapGroup(""/local"")]
public class LocalGroup { }

[MapGet(""/x"", Group = typeof(LocalGroup))]
public class HostEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var diagnostics = CompilationUtilities.GetDiagnostics(code);

        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP009");
    }

    [Fact]
    public void SourceGroupWithCrossAssemblyParent_NotOptedIn_ReportsMINEP009()
    {
        var hostSource = @"
namespace Host;

[MapGroup(""/sub"", ParentGroup = typeof(Ext.LibGroup))]
public class SubGroup { }";

        var diagnostics = DiagnosticsFor(hostSource, BuildGroupLibrary());

        Assert.Contains(diagnostics, d => d.Id == "MINEP009");
    }

    [Fact]
    public void EndpointReferencesNonPublicCrossAssemblyGroup_NoMINEP009()
    {
        // A non-public referenced group can't be named from host source at all (the typeof is itself
        // CS0122), so the compiler already reports it — MINEP009 must not pile on redundantly.
        var lib = new CompilationBuilder(@"
namespace Ext;

[MapGroup(""/hidden"")]
internal class HiddenGroup { }", assemblyName: "Ext.Lib").WithMvcReferences().Build();

        var hostSource = @"
namespace Host;

[MapGet(""/x"", Group = typeof(Ext.HiddenGroup))]
public class HostEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var diagnostics = DiagnosticsFor(hostSource, lib);

        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP009");
    }
}
