using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;
using MinimalEndpoints.CodeGeneration.Groups.Models;
using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration.Endpoints.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EndpointsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            Diagnostics.MissingEntryPoint,
            Diagnostics.MultipleAttributesDetected,
            Diagnostics.ServiceTypeMissingEntryPoint,
            Diagnostics.InvalidGroupType,
            Diagnostics.UnsupportedEndpointShape);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Early exit before binding the semantic model: a part with zero attribute lists can never
        // carry a Map attribute (attributes live only on declarations), so it cannot be an endpoint.
        // This skips the GetDeclaredSymbol bind for every attribute-less class in the compilation.
        if (classDeclaration.AttributeLists.Count == 0)
        {
            return;
        }

        var classSymbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDeclaration);
        if (classSymbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return;
        }

        // Skip abstract classes at the symbol level (the merged symbol is authoritative — the old
        // per-part syntax check missed `abstract` declared on another partial part). Abstract
        // endpoints are a never-mapped base pattern, so no diagnostic is reported.
        if (namedTypeSymbol.IsAbstract)
        {
            return;
        }

        var attributes = namedTypeSymbol.GetAttributes()
            .Where(EndpointDefinition.Factory.Predicate)
            .ToArray();

        if (attributes.Length == 0)
        {
            return;
        }

        // The merged symbol's attributes are seen by EVERY partial part, so without this each
        // diagnostic would be reported once per part (including parts carrying no Map attribute).
        // Report only from the part(s) that actually declare a Map attribute.
        if (!CurrentPartCarriesMapAttribute(classDeclaration, attributes, context.CancellationToken))
        {
            return;
        }

        if (attributes.Length > 1)
        {
            // The Map attributes may be split across parts; report MINEP002 exactly once, from the
            // part holding the first Map attribute in deterministic source order.
            if (CurrentPartHasFirstMapAttribute(classDeclaration, attributes, context.CancellationToken))
            {
                var multipleAttributesDiagnostic = Diagnostic.Create(
                    Diagnostics.MultipleAttributesDetected,
                    classDeclaration.Identifier.GetLocation(),
                    namedTypeSymbol.Name
                );

                context.ReportDiagnostic(multipleAttributesDiagnostic);
            }

            return;
        }

        // The class is intended as a single endpoint but its shape may prevent the generator from
        // referencing it (open generic, file-local, sub-internal). Report MINEP008 and stop — these
        // are never mapped, so the entry-point / service-type / group checks below do not apply.
        var shapeRejection = SymbolDefinitionFactory.ClassifyShape(namedTypeSymbol);
        if (shapeRejection != ShapeRejection.None)
        {
            var unsupportedShapeDiagnostic = Diagnostic.Create(
                Diagnostics.UnsupportedEndpointShape,
                classDeclaration.Identifier.GetLocation(),
                namedTypeSymbol.Name,
                SymbolDefinitionFactory.DescribeShapeRejection(shapeRejection)
            );

            context.ReportDiagnostic(unsupportedShapeDiagnostic);
            return;
        }

        var mapMethodsAttributeDefinition = attributes[0].GetMapMethodAttributeDefinition();
        if (mapMethodsAttributeDefinition == null)
        {
            // The attribute is mid-typing / malformed (e.g. '[MapGet]' with no pattern). The
            // compiler already reports the incomplete attribute; skip analysis so we don't
            // dereference a null definition and crash the analyzer (AD0001) on every keystroke.
            return;
        }

        var entryPoint = namedTypeSymbol.FindEntryPointMethod(mapMethodsAttributeDefinition.EntryPoint);

        if (entryPoint == null)
        {
            // Hand the requested (custom) entry-point name to the code fix via the properties bag so it
            // can generate a method with that exact name instead of always emitting Handle/HandleAsync.
            var properties = string.IsNullOrEmpty(mapMethodsAttributeDefinition.EntryPoint)
                ? ImmutableDictionary<string, string>.Empty
                : ImmutableDictionary<string, string>.Empty.Add("EntryPoint", mapMethodsAttributeDefinition.EntryPoint);

            var missingEntryPointDiagnostic = Diagnostic.Create(
                Diagnostics.MissingEntryPoint,
                classDeclaration.Identifier.GetLocation(),
                properties,
                namedTypeSymbol.Name,
                FormatAttributeName(attributes[0])
            );

            context.ReportDiagnostic(missingEntryPointDiagnostic);
            return;
        }

        if (!string.IsNullOrEmpty(mapMethodsAttributeDefinition.ServiceName))
        {
            var serviceTypeSymbol = GetServiceTypeSymbol(attributes[0]);
            if (serviceTypeSymbol != null)
            {
                // The generator types the resolved instance as the ServiceType and calls
                // instance.{EntryPoint}(args). For that call to bind, the ServiceType must expose a
                // method with the entry point's NAME and a COMPATIBLE signature — matching by name
                // alone let an incompatible overload pass analysis yet miscompile the generated call.
                var interfaceHasMethod = ServiceTypeHasCompatibleEntryPoint(serviceTypeSymbol, entryPoint);

                if (!interfaceHasMethod)
                {
                    var serviceTypeDiagnostic = Diagnostic.Create(
                        Diagnostics.ServiceTypeMissingEntryPoint,
                        classDeclaration.Identifier.GetLocation(),
                        serviceTypeSymbol.Name,
                        namedTypeSymbol.Name,
                        entryPoint.Name
                    );

                    context.ReportDiagnostic(serviceTypeDiagnostic);
                }
            }
        }

        // Validate Group if specified
        var groupTypeSymbol = GetGroupTypeSymbol(attributes[0]);
        if (groupTypeSymbol != null)
        {
            ValidateGroupType(context, classDeclaration, namedTypeSymbol, groupTypeSymbol);
        }
    }

    /// <summary>
    /// True when at least one of the symbol's Map attributes is declared on THIS partial part.
    /// Used to report each diagnostic once (on a Map-attributed part) rather than once per part.
    /// </summary>
    private static bool CurrentPartCarriesMapAttribute(
        ClassDeclarationSyntax declaration, AttributeData[] mapAttributes, CancellationToken cancellationToken)
    {
        foreach (var attribute in mapAttributes)
        {
            var syntax = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken);
            if (syntax != null && syntax.FirstAncestorOrSelf<ClassDeclarationSyntax>() == declaration)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True when the FIRST Map attribute in deterministic source order is declared on THIS part.
    /// Lets MINEP002 fire exactly once when the duplicate Map attributes are split across parts.
    /// </summary>
    private static bool CurrentPartHasFirstMapAttribute(
        ClassDeclarationSyntax declaration, AttributeData[] mapAttributes, CancellationToken cancellationToken)
    {
        SyntaxNode firstSyntax = null;
        foreach (var attribute in mapAttributes)
        {
            var syntax = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken);
            if (syntax == null)
            {
                continue;
            }

            if (firstSyntax == null || ComesBeforeInSource(syntax, firstSyntax))
            {
                firstSyntax = syntax;
            }
        }

        return firstSyntax != null && firstSyntax.FirstAncestorOrSelf<ClassDeclarationSyntax>() == declaration;
    }

    private static bool ComesBeforeInSource(SyntaxNode left, SyntaxNode right)
    {
        var pathComparison = string.CompareOrdinal(left.SyntaxTree.FilePath, right.SyntaxTree.FilePath);
        return pathComparison != 0 ? pathComparison < 0 : left.SpanStart < right.SpanStart;
    }

    private static void ValidateGroupType(
        SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDeclaration,
        INamedTypeSymbol endpointSymbol,
        INamedTypeSymbol groupTypeSymbol)
    {
        // Check if group type has MapGroupAttribute. Use Any (only existence matters): a group
        // type may carry a duplicated [MapGroup] across partial parts — a compiler error (CS0579),
        // but GetAttributes() returns both, and SingleOrDefault(predicate) would throw on >1 match
        // and crash the analyzer (AD0001).
        var hasMapGroupAttribute = groupTypeSymbol.GetAttributes()
            .Any(EndpointGroupDefinition.Factory.Predicate);

        if (!hasMapGroupAttribute)
        {
            var diagnostic = Diagnostic.Create(
                Diagnostics.InvalidGroupType,
                classDeclaration.Identifier.GetLocation(),
                groupTypeSymbol.Name,
                endpointSymbol.Name
            );

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Renders an attribute's name as written in source — without the "Attribute" suffix, wrapped in
    /// brackets (e.g. "[MapGet]") — for the MINEP001 message. The descriptor's {1} placeholder names
    /// the attribute the class is marked with; passing the (usually null) custom EntryPoint there
    /// produced "marked with  but …" with a blank and a double space.
    /// </summary>
    private static string FormatAttributeName(AttributeData attribute)
    {
        var name = attribute.AttributeClass?.Name ?? "Map";
        const string suffix = "Attribute";
        if (name.EndsWith(suffix, StringComparison.Ordinal))
        {
            name = name.Substring(0, name.Length - suffix.Length);
        }

        return $"[{name}]";
    }

    /// <summary>
    /// True when <paramref name="serviceType"/> (its own members, its base-class chain, or any
    /// interface it implements/extends) exposes a public, non-static method whose name AND parameter
    /// signature are compatible with <paramref name="entryPoint"/> — i.e. the generated
    /// <c>instance.{EntryPoint}(args)</c> call would bind.
    /// </summary>
    private static bool ServiceTypeHasCompatibleEntryPoint(INamedTypeSymbol serviceType, IMethodSymbol entryPoint)
    {
        foreach (var candidate in EnumerateCandidateMethods(serviceType))
        {
            if (candidate.Name == entryPoint.Name &&
                !candidate.IsStatic &&
                candidate.DeclaredAccessibility == Accessibility.Public &&
                ParametersMatch(candidate, entryPoint))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Yields every method visible on the service type: its own members, those of its base-class
    /// chain (so a class ServiceType with an inherited entry point is not a false positive), and
    /// those of every interface it implements or extends (AllInterfaces is transitive).
    /// </summary>
    private static IEnumerable<IMethodSymbol> EnumerateCandidateMethods(INamedTypeSymbol serviceType)
    {
        for (INamedTypeSymbol current = serviceType; current != null; current = current.BaseType)
        {
            foreach (var method in current.GetMembers().OfType<IMethodSymbol>())
            {
                yield return method;
            }
        }

        foreach (var @interface in serviceType.AllInterfaces)
        {
            foreach (var method in @interface.GetMembers().OfType<IMethodSymbol>())
            {
                yield return method;
            }
        }
    }

    private static bool ParametersMatch(IMethodSymbol candidate, IMethodSymbol entryPoint)
    {
        if (candidate.Parameters.Length != entryPoint.Parameters.Length)
        {
            return false;
        }

        for (var i = 0; i < candidate.Parameters.Length; i++)
        {
            // Compare by type identity (nullability-agnostic): the generated positional call binds
            // regardless of reference-nullability annotations, but not across distinct types.
            if (!SymbolEqualityComparer.Default.Equals(
                    candidate.Parameters[i].Type, entryPoint.Parameters[i].Type))
            {
                return false;
            }
        }

        return true;
    }

    private static INamedTypeSymbol GetServiceTypeSymbol(AttributeData attributeData)
    {
        var serviceTypeArg = attributeData.NamedArguments
            .FirstOrDefault(arg => arg.Key == "ServiceType");

        if (serviceTypeArg.Value.Value is INamedTypeSymbol serviceType)
        {
            return serviceType;
        }

        return null;
    }

    private static INamedTypeSymbol GetGroupTypeSymbol(AttributeData attributeData)
    {
        var groupArg = attributeData.NamedArguments
            .FirstOrDefault(arg => arg.Key == "Group");

        if (groupArg.Value.Value is INamedTypeSymbol groupType)
        {
            return groupType;
        }

        return null;
    }
}
