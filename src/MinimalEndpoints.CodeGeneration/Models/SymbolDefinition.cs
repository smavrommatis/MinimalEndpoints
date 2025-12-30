using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.CodeGeneration.Models;

internal abstract class SymbolDefinition
{
    protected SymbolDefinition(INamedTypeSymbol symbol)
    {
        Symbol = symbol;
    }

    public INamedTypeSymbol Symbol { get; }
}
