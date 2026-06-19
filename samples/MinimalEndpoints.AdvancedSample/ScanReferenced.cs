using MinimalEndpoints.Annotations;

// Opt into cross-assembly discovery: endpoints and groups from the referenced
// MinimalEndpoints.ExternalSample library are registered and mapped by the generated
// AddMinimalEndpoints()/UseMinimalEndpoints() as if they were declared here — so /external/info works
// without this project declaring it. See docs/diagnostics/MINEP009.md for the opt-in and its diagnostics.
[assembly: ScanReferencedEndpoints]
