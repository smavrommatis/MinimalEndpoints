// netstandard2.0 (the analyzer/generator target) does not ship System.Runtime.CompilerServices.IsExternalInit,
// which the compiler requires to emit `init`-only property setters. This internal polyfill makes `init`
// usable here without bumping the target framework. It is compiler-only plumbing — no runtime behaviour.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
