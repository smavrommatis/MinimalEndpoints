namespace MinimalEndpoints.Annotations;

/// <summary>
/// Opts the host assembly into cross-assembly endpoint and group discovery. Apply this once at the
/// assembly level and the source generator will scan the host's referenced compiled assemblies for
/// <c>[Map*]</c> endpoint and <c>[MapGroup]</c> group types and register/map them alongside the
/// host's own, including composing groups across the assembly boundary.
/// </summary>
/// <remarks>
/// By default the generator only discovers endpoints/groups declared in the current compilation —
/// endpoints in a referenced compiled assembly (a project or NuGet reference) are not registered.
/// This attribute turns on the (otherwise zero-cost) reference scan. Discovery still happens entirely
/// at compile time with no runtime reflection.
/// <para>
/// Referenced endpoint and group classes must be <see langword="public"/> (the host's generated code
/// references them across the assembly boundary); non-public ones are skipped. A non-public
/// <c>ServiceType</c> is ignored — the endpoint is registered as its concrete class instead.
/// </para>
/// <para>
/// Only assemblies the host references DIRECTLY and that reference MinimalEndpoints are scanned; the
/// BCL, ASP.NET Core, unrelated packages, and purely transitive references are never enumerated.
/// </para>
/// <para>
/// By default (no arguments) ALL referenced assemblies that use MinimalEndpoints are scanned. To
/// restrict scanning to specific assemblies, pass one or more marker types — any type from each
/// target assembly works (e.g. <c>typeof(SomeEndpointInThatAssembly)</c>); only those assemblies are
/// then scanned.
/// </para>
/// <example>
/// <code>
/// // Scan every referenced assembly that uses MinimalEndpoints:
/// [assembly: MinimalEndpoints.Annotations.ScanReferencedEndpoints]
///
/// // Scan ONLY the assemblies containing the given marker types:
/// [assembly: MinimalEndpoints.Annotations.ScanReferencedEndpoints(typeof(MyCompany.Api.SomeEndpoint))]
///
/// // Endpoints/groups defined in the scanned libraries are then registered by
/// // AddMinimalEndpoints()/UseMinimalEndpoints() with no further changes.
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ScanReferencedEndpointsAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute. With no <paramref name="assemblyMarkers"/> every referenced assembly
    /// that uses MinimalEndpoints is scanned; otherwise only the assemblies that contain the given
    /// marker types are scanned.
    /// </summary>
    /// <param name="assemblyMarkers">
    /// Optional marker types whose containing assemblies should be scanned. Any type from a target
    /// assembly works.
    /// </param>
    public ScanReferencedEndpointsAttribute(params Type[] assemblyMarkers)
    {
        AssemblyMarkers = assemblyMarkers ?? Array.Empty<Type>();
    }

    /// <summary>
    /// The marker types whose containing assemblies are scanned. Empty means scan ALL referenced
    /// assemblies that use MinimalEndpoints.
    /// </summary>
    public Type[] AssemblyMarkers { get; }
}
