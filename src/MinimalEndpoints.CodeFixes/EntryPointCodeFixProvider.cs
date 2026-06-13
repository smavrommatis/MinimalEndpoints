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
public class EntryPointCodeFixProvider : CodeFixProvider
{
    // ImmutableArray.Create rather than a collection expression: the lowered Roslyn floor (4.8.0)
    // brings a System.Collections.Immutable whose ImmutableArray<T> predates collection-expression
    // support (CS9210).
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("MINEP001");

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var classDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDeclaration == null)
        {
            return;
        }

        // A custom EntryPoint name (when the user set one) is carried on the diagnostic by the analyzer.
        // Generate methods with that name so applying the fix actually resolves MINEP001; otherwise fall
        // back to the conventional Handle/HandleAsync names.
        diagnostic.Properties.TryGetValue("EntryPoint", out var entryPoint);

        // A custom EntryPoint that is not a usable C# identifier cannot be emitted as a method name
        // (it would produce uncompilable code that never resolves MINEP001). Ignore it and fall back
        // to the conventional names. Reserved keywords ARE valid identifiers here — they are emitted
        // verbatim (e.g. "@class") at generation time.
        if (!string.IsNullOrEmpty(entryPoint) && !SyntaxFacts.IsValidIdentifier(entryPoint))
        {
            entryPoint = null;
        }

        var syncName = string.IsNullOrEmpty(entryPoint) ? "Handle" : entryPoint;
        var asyncName = string.IsNullOrEmpty(entryPoint) ? "HandleAsync" : entryPoint;

        // Only offer an action whose method name does not already collide with a member the class
        // declares. MINEP001 fires precisely when there is no VALID entry point, which commonly means
        // an existing non-public/static Handle (or HandleAsync). Adding a same-named zero-arg method
        // there would emit uncompilable code (CS0111 duplicate member, or CS0102 against a field/
        // property), so the colliding action is suppressed; the non-colliding alternative remains.
        if (!ClassAlreadyDeclaresMember(classDeclaration, syncName))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Add {syncName} method",
                    createChangedDocument: _ => AddHandleMethod(context.Document, root, classDeclaration, syncName),
                    equivalenceKey: "AddHandleMethod"),
                diagnostic);
        }

        if (!ClassAlreadyDeclaresMember(classDeclaration, asyncName))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Add {asyncName} method (returns Task)",
                    createChangedDocument: _ => AddHandleAsyncMethod(context.Document, root, classDeclaration, asyncName),
                    equivalenceKey: "AddHandleAsyncMethod"),
                diagnostic);
        }
    }

    /// <summary>
    /// True when <paramref name="classDeclaration"/> already declares a member that the generated
    /// zero-parameter method would collide with: a zero-parameter method of the same name (CS0111),
    /// or a property/event/field declaring that name (CS0102). Such a member is exactly the
    /// non-viable entry point that triggered MINEP001, so re-adding it cannot resolve the diagnostic.
    /// </summary>
    private static bool ClassAlreadyDeclaresMember(ClassDeclarationSyntax classDeclaration, string name)
    {
        foreach (var member in classDeclaration.Members)
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

    private static Task<Document> AddHandleMethod(Document document, SyntaxNode root,
        ClassDeclarationSyntax classDeclaration, string methodName)
    {
        var handleMethod = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName("IResult"),
                MethodIdentifier(methodName))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.ParseExpression("Results.Ok()"))))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newClass = classDeclaration.AddMembers(handleMethod);
        // Reuse the root already fetched and null-checked in RegisterCodeFixesAsync instead of
        // re-fetching and dereferencing it unchecked.
        var newRoot = root.ReplaceNode(classDeclaration, newClass);

        newRoot = EnsureUsingDirectives(newRoot, "Microsoft.AspNetCore.Http");

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static Task<Document> AddHandleAsyncMethod(Document document, SyntaxNode root,
        ClassDeclarationSyntax classDeclaration, string methodName)
    {
        // Emit a non-async method returning Task.FromResult(...) rather than `async` with no `await`,
        // which would warn CS1998 in the user's code.
        var handleMethod = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.GenericName("Task")
                    .AddTypeArgumentListArguments(SyntaxFactory.IdentifierName("IResult")),
                MethodIdentifier(methodName))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.ParseExpression("Task.FromResult(Results.Ok())"))))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newClass = classDeclaration.AddMembers(handleMethod);
        var newRoot = root.ReplaceNode(classDeclaration, newClass);

        // Task<IResult> requires System.Threading.Tasks in addition to the Microsoft.AspNetCore.Http
        // namespace that provides IResult/Results.
        newRoot = EnsureUsingDirectives(newRoot, "Microsoft.AspNetCore.Http", "System.Threading.Tasks");

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
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
}

