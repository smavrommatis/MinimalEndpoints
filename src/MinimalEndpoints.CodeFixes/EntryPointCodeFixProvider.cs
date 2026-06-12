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
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ["MINEP001"];

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
        var syncName = string.IsNullOrEmpty(entryPoint) ? "Handle" : entryPoint;
        var asyncName = string.IsNullOrEmpty(entryPoint) ? "HandleAsync" : entryPoint;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Add {syncName} method",
                createChangedDocument: c => AddHandleMethodAsync(context.Document, classDeclaration, syncName, c),
                equivalenceKey: "AddHandleMethod"),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Add {asyncName} method (async)",
                createChangedDocument: c => AddHandleAsyncMethodAsync(context.Document, classDeclaration, asyncName, c),
                equivalenceKey: "AddHandleAsyncMethod"),
            diagnostic);
    }

    private static async Task<Document> AddHandleMethodAsync(Document document, ClassDeclarationSyntax classDeclaration,
        string methodName, CancellationToken cancellationToken)
    {
        var handleMethod = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName("IResult"),
                methodName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.ParseExpression("Results.Ok()"))))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newClass = classDeclaration.AddMembers(handleMethod);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root.ReplaceNode(classDeclaration, newClass);

        newRoot = EnsureUsingDirectives(newRoot, "Microsoft.AspNetCore.Http");

        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> AddHandleAsyncMethodAsync(Document document,
        ClassDeclarationSyntax classDeclaration, string methodName, CancellationToken cancellationToken)
    {
        // Emit a non-async method returning Task.FromResult(...) rather than `async` with no `await`,
        // which would warn CS1998 in the user's code.
        var handleMethod = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.GenericName("Task")
                    .AddTypeArgumentListArguments(SyntaxFactory.IdentifierName("IResult")),
                methodName)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.ParseExpression("Task.FromResult(Results.Ok())"))))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newClass = classDeclaration.AddMembers(handleMethod);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root.ReplaceNode(classDeclaration, newClass);

        // Task<IResult> requires System.Threading.Tasks in addition to the Microsoft.AspNetCore.Http
        // namespace that provides IResult/Results.
        newRoot = EnsureUsingDirectives(newRoot, "Microsoft.AspNetCore.Http", "System.Threading.Tasks");

        return document.WithSyntaxRoot(newRoot);
    }

    private static SyntaxNode EnsureUsingDirectives(SyntaxNode root, params string[] requiredUsings)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return root;
        }

        var existingUsings = new HashSet<string>(
            compilationUnit.Usings.Select(u => u.Name!.ToString()));

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

