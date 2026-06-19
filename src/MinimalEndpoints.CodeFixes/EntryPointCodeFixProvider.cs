using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;


namespace MinimalEndpoints.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EntryPointCodeFixProvider)), Shared]
public sealed class EntryPointCodeFixProvider : CodeFixProvider
{
    private const string AddHandleEquivalenceKey = "AddHandleMethod";
    private const string AddHandleAsyncEquivalenceKey = "AddHandleAsyncMethod";

    // ImmutableArray.Create rather than a collection expression: the lowered Roslyn floor (4.8.0)
    // brings a System.Collections.Immutable whose ImmutableArray<T> predates collection-expression
    // support (CS9210).
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("MINEP001");

    // Document-based (not the BatchFixer): a Fix-All over several endpoints in ONE file applies every
    // member addition and the shared `using` insertion in a single pass over one root, so the using is
    // added exactly once. The BatchFixer computes each fix against the original root independently, which
    // can insert the same `using Microsoft.AspNetCore.Http;` more than once on a multi-endpoint file.
    public override FixAllProvider GetFixAllProvider() => new EntryPointFixAllProvider();

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        // FixableDiagnosticIds has a single id and Roslyn guarantees Diagnostics is non-empty, so index
        // directly (it is an ImmutableArray) rather than allocating a LINQ enumerator via First().
        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Endpoints may be records too (a record class), so resolve the enclosing type declaration
        // generically rather than only ClassDeclarationSyntax.
        var typeDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (typeDeclaration == null)
        {
            return;
        }

        var syncName = ResolveMethodName(diagnostic, isAsync: false);
        var asyncName = ResolveMethodName(diagnostic, isAsync: true);

        // Only offer an action whose method name does not already collide with a member the type
        // declares. MINEP001 fires precisely when there is no VALID entry point, which commonly means
        // an existing non-public/static Handle (or HandleAsync). Adding a same-named zero-arg method
        // there would emit uncompilable code (CS0111 duplicate member, or CS0102 against a field/
        // property), so the colliding action is suppressed; the non-colliding alternative remains.
        if (!TypeAlreadyDeclaresMember(typeDeclaration, syncName))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Add {syncName} method",
                    createChangedDocument: _ => AddEntryPointMethod(context.Document, root, typeDeclaration, syncName, isAsync: false),
                    equivalenceKey: AddHandleEquivalenceKey),
                diagnostic);
        }

        if (!TypeAlreadyDeclaresMember(typeDeclaration, asyncName))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Add {asyncName} method (returns Task)",
                    createChangedDocument: _ => AddEntryPointMethod(context.Document, root, typeDeclaration, asyncName, isAsync: true),
                    equivalenceKey: AddHandleAsyncEquivalenceKey),
                diagnostic);
        }
    }

    /// <summary>
    /// Resolves the method name a fix would emit for <paramref name="diagnostic"/>. A custom EntryPoint
    /// name (carried on the diagnostic by the analyzer) is used when present and a valid C# identifier;
    /// otherwise the conventional Handle/HandleAsync name is used. Reserved keywords ARE valid identifiers
    /// here — they are emitted verbatim (e.g. "@class") via <see cref="MethodIdentifier"/>.
    /// </summary>
    private static string ResolveMethodName(Diagnostic diagnostic, bool isAsync)
    {
        diagnostic.Properties.TryGetValue("EntryPoint", out var entryPoint);

        if (!string.IsNullOrEmpty(entryPoint) && !SyntaxFacts.IsValidIdentifier(entryPoint))
        {
            entryPoint = null;
        }

        if (!string.IsNullOrEmpty(entryPoint))
        {
            return entryPoint;
        }

        return isAsync ? "HandleAsync" : "Handle";
    }

    /// <summary>
    /// True when <paramref name="typeDeclaration"/> already declares a member that the generated
    /// zero-parameter method would collide with: a zero-parameter method of the same name (CS0111),
    /// or a property/event/field declaring that name (CS0102). Such a member is exactly the
    /// non-viable entry point that triggered MINEP001, so re-adding it cannot resolve the diagnostic.
    /// </summary>
    private static bool TypeAlreadyDeclaresMember(TypeDeclarationSyntax typeDeclaration, string name)
    {
        foreach (var member in typeDeclaration.Members)
        {
            switch (member)
            {
                case MethodDeclarationSyntax method
                    when method.Identifier.ValueText == name && method.ParameterList.Parameters.Count == 0:
                    return true;
                case PropertyDeclarationSyntax property when property.Identifier.ValueText == name:
                    return true;
                case EventDeclarationSyntax @event when @event.Identifier.ValueText == name:
                    return true;
                case BaseFieldDeclarationSyntax field
                    when field.Declaration.Variables.Any(v => v.Identifier.ValueText == name):
                    return true;
            }
        }

        return false;
    }

    private static Task<Document> AddEntryPointMethod(Document document, SyntaxNode root,
        TypeDeclarationSyntax typeDeclaration, string methodName, bool isAsync)
    {
        var newType = AddEntryPointMember(typeDeclaration, BuildEntryPointMethod(methodName, isAsync));
        // Reuse the root already fetched and null-checked in RegisterCodeFixesAsync instead of
        // re-fetching and dereferencing it unchecked.
        var newRoot = EnsureEntryPointUsings(root.ReplaceNode(typeDeclaration, newType), isAsync);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    /// <summary>
    /// Adds <paramref name="method"/> to <paramref name="type"/>, first giving a semicolon-bodied
    /// (positional) record a braced body: <c>record Foo(int X);</c> has no member-list braces, so a bare
    /// <c>AddMembers</c> would append the method after the <c>;</c> and emit uncompilable text.
    /// </summary>
    private static TypeDeclarationSyntax AddEntryPointMember(TypeDeclarationSyntax type, MethodDeclarationSyntax method)
    {
        if (type.OpenBraceToken.IsKind(SyntaxKind.None))
        {
            type = type
                .WithSemicolonToken(default)
                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        return type.AddMembers(method);
    }

    /// <summary>
    /// Adds the namespaces the generated entry-point method needs — Microsoft.AspNetCore.Http for
    /// IResult/Results, plus System.Threading.Tasks for the async <c>Task&lt;IResult&gt;</c> form —
    /// exactly once. Shared by the single-fix and Fix-All paths so they cannot diverge on the usings.
    /// </summary>
    private static SyntaxNode EnsureEntryPointUsings(SyntaxNode root, bool isAsync) =>
        isAsync
            ? EnsureUsingDirectives(root, "Microsoft.AspNetCore.Http", "System.Threading.Tasks")
            : EnsureUsingDirectives(root, "Microsoft.AspNetCore.Http");

    /// <summary>
    /// Builds the entry-point method to insert. The sync form returns <c>IResult</c> via
    /// <c>Results.Ok()</c>; the async form returns <c>Task&lt;IResult&gt;</c> via
    /// <c>Task.FromResult(Results.Ok())</c> — a non-async method (rather than <c>async</c> with no
    /// <c>await</c>, which would warn CS1998 in the user's code).
    /// </summary>
    private static MethodDeclarationSyntax BuildEntryPointMethod(string methodName, bool isAsync)
    {
        TypeSyntax returnType = isAsync
            ? SyntaxFactory.GenericName("Task").AddTypeArgumentListArguments(SyntaxFactory.IdentifierName("IResult"))
            : SyntaxFactory.IdentifierName("IResult");

        var returnExpression = isAsync ? "Task.FromResult(Results.Ok())" : "Results.Ok()";

        return SyntaxFactory.MethodDeclaration(returnType, MethodIdentifier(methodName))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.ReturnStatement(SyntaxFactory.ParseExpression(returnExpression))))
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    /// <summary>
    /// Builds the method-name identifier, prefixing <c>@</c> when the name is a reserved C# keyword
    /// (e.g. a custom <c>EntryPoint = "class"</c> emits <c>@class</c>) so the generated method is a
    /// valid identifier and the analyzer still matches it by name. The verbatim token is produced via
    /// the lexer (ParseToken) so its ValueText is the unescaped name ("class"), matching the token a
    /// reparse of the emitted text would yield.
    /// </summary>
    private static SyntaxToken MethodIdentifier(string name) =>
        SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None
            ? SyntaxFactory.ParseToken("@" + name)
            : SyntaxFactory.Identifier(name);

    private static SyntaxNode EnsureUsingDirectives(SyntaxNode root, params string[] requiredUsings)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return root;
        }

        // Skip directives with a null Name: since C# 12 a using can alias a NON-name type (tuple,
        // array, pointer), whose Name is null because the type lives on NamespaceOrType. Dereferencing
        // it (even with the null-forgiving operator) threw an NRE, silently failing the fix.
        var existingUsings = new HashSet<string>(
            compilationUnit.Usings
                .Select(u => u.Name?.ToString())
                .Where(name => name != null));

        // Accumulate on the evolving compilation unit (and dedupe) so that every missing using is added,
        // in the order given — not just the last one.
        foreach (var requiredUsing in requiredUsings)
        {
            if (existingUsings.Add(requiredUsing))
            {
                compilationUnit = compilationUnit.AddUsings(
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(requiredUsing))
                        .WithAdditionalAnnotations(Formatter.Annotation));
            }
        }

        return compilationUnit;
    }

    /// <summary>
    /// Fixes every MINEP001 in a document in a single pass: it adds the chosen entry-point method to each
    /// endpoint type and inserts the required usings ONCE, avoiding the duplicate-using hazard of applying
    /// independent per-diagnostic edits to the original root.
    /// </summary>
    private sealed class EntryPointFixAllProvider : DocumentBasedFixAllProvider
    {
        protected override async Task<Document> FixAllAsync(
            FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
        {
            var root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return document;
            }

            var isAsync = fixAllContext.CodeActionEquivalenceKey == AddHandleAsyncEquivalenceKey;

            // Resolve each MINEP001 to the endpoint type + method name it would add, skipping a type that
            // already declares that member (the single-fix path would not have offered the action there).
            var targets = new List<(TypeDeclarationSyntax Type, string Name)>();
            foreach (var diagnostic in diagnostics)
            {
                var typeDeclaration = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent?
                    .AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
                if (typeDeclaration == null)
                {
                    continue;
                }

                var methodName = ResolveMethodName(diagnostic, isAsync);
                if (!TypeAlreadyDeclaresMember(typeDeclaration, methodName))
                {
                    targets.Add((typeDeclaration, methodName));
                }
            }

            if (targets.Count == 0)
            {
                return document;
            }

            // Track the target type nodes so each member addition sees the edits made to the others, then
            // add the required usings once over the fully-edited root.
            var distinctTypes = targets.Select(t => t.Type).Distinct().ToArray();
            var newRoot = root.TrackNodes(distinctTypes);
            foreach (var (typeDeclaration, methodName) in targets)
            {
                var tracked = newRoot.GetCurrentNode(typeDeclaration);
                if (tracked == null)
                {
                    continue;
                }

                newRoot = newRoot.ReplaceNode(tracked, AddEntryPointMember(tracked, BuildEntryPointMethod(methodName, isAsync)));
            }

            newRoot = EnsureEntryPointUsings(newRoot, isAsync);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
