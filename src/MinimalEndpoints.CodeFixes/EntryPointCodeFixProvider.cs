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
            .OfType<ClassDeclarationSyntax>().First();
        if (classDeclaration == null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add Handle method",
                createChangedDocument: c => AddHandleMethodAsync(context.Document, classDeclaration, c),
                equivalenceKey: "AddHandleMethod"),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add HandleAsync method",
                createChangedDocument: c => AddHandleAsyncMethodAsync(context.Document, classDeclaration, c),
                equivalenceKey: "AddHandleAsyncMethod"),
            diagnostic);
    }

    private static async Task<Document> AddHandleMethodAsync(Document document, ClassDeclarationSyntax classDeclaration,
        CancellationToken cancellationToken)
    {
        var handleMethod = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.IdentifierName("IResult"),
                "Handle")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.ParseExpression("Results.Ok()"))))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newClass = classDeclaration.AddMembers(handleMethod);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root.ReplaceNode(classDeclaration, newClass);

        newRoot = EnsureUsingDirectives(newRoot, ["Microsoft.AspNetCore.Http"]);

        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> AddHandleAsyncMethodAsync(Document document,
        ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
    {
        var handleMethod = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.GenericName("Task")
                    .AddTypeArgumentListArguments(SyntaxFactory.IdentifierName("IResult")),
                "HandleAsync")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
            .WithBody(SyntaxFactory.Block(
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.ParseExpression("Results.Ok()"))))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var newClass = classDeclaration.AddMembers(handleMethod);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root.ReplaceNode(classDeclaration, newClass);

        newRoot = EnsureUsingDirectives(newRoot, ["Microsoft.AspNetCore.Http"]);


        return document.WithSyntaxRoot(newRoot);
    }

    private static SyntaxNode EnsureUsingDirectives(SyntaxNode root, HashSet<string> requiredUsings)
    {
        if (root is CompilationUnitSyntax compilationUnit)
        {
            var existingUsings = new HashSet<string>(
                compilationUnit.Usings.Select(u => u.Name!.ToString()));

            var missingUsings = requiredUsings.Except(existingUsings)
                .Select(x => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(x)));

            foreach (var missingUsing in missingUsings)
            {
                root = compilationUnit.AddUsings(missingUsing.WithAdditionalAnnotations(Formatter.Annotation));
            }
        }

        return root;
    }
}

