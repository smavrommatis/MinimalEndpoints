using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MinimalEndpoints.Tests.Common;

/// <summary>
/// Shared helpers for resolving <see cref="INamedTypeSymbol"/>s out of a single-tree
/// compilation. Previously copied byte-for-byte across several model test classes.
/// </summary>
public static class SymbolTestHelpers
{
    public static INamedTypeSymbol GetClassSymbol(Compilation compilation, string className)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == className);

        return (semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol)!;
    }

    public static INamedTypeSymbol GetNestedClassSymbol(Compilation compilation, string outerClassName, string innerClassName)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        var outerClass = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == outerClassName);

        var innerClass = outerClass.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == innerClassName);

        return (semanticModel.GetDeclaredSymbol(innerClass) as INamedTypeSymbol)!;
    }
}
