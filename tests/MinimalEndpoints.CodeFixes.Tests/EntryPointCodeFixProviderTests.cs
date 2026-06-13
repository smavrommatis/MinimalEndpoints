namespace MinimalEndpoints.CodeFixes.Tests;

/// <summary>
/// Tests for <see cref="MinimalEndpoints.CodeFixes.EntryPointCodeFixProvider"/> — the MINEP001
/// "add an entry-point method" code fix.
/// </summary>
public class EntryPointCodeFixProviderTests
{
    [Fact]
    public async Task AddHandle_ResolvesMinep001_ProducesExpectedDocument()
    {
        const string test = """
            using MinimalEndpoints.Annotations;

            namespace TestApp;

            [MapGet("/test")]
            public class {|MINEP001:TestEndpoint|}
            {
            }
            """;

        const string fixedCode = """
            using MinimalEndpoints.Annotations;
            using Microsoft.AspNetCore.Http;

            namespace TestApp;

            [MapGet("/test")]
            public class TestEndpoint
            {
                public IResult Handle()
                {
                    return Results.Ok();
                }
            }
            """;

        await new MinEndpointsCodeFixTest
        {
            TestCode = test.ReplaceLineEndings(),
            FixedCode = fixedCode.ReplaceLineEndings(),
            CodeActionEquivalenceKey = "AddHandleMethod",
        }.RunAsync();
    }

    [Fact]
    public async Task AddHandle_UsingAlreadyPresent_IsNotDuplicated()
    {
        const string test = """
            using MinimalEndpoints.Annotations;
            using Microsoft.AspNetCore.Http;

            namespace TestApp;

            [MapGet("/test")]
            public class {|MINEP001:TestEndpoint|}
            {
            }
            """;

        const string fixedCode = """
            using MinimalEndpoints.Annotations;
            using Microsoft.AspNetCore.Http;

            namespace TestApp;

            [MapGet("/test")]
            public class TestEndpoint
            {
                public IResult Handle()
                {
                    return Results.Ok();
                }
            }
            """;

        await new MinEndpointsCodeFixTest
        {
            TestCode = test.ReplaceLineEndings(),
            FixedCode = fixedCode.ReplaceLineEndings(),
            CodeActionEquivalenceKey = "AddHandleMethod",
        }.RunAsync();
    }

    [Fact]
    public async Task FixAll_TwoEndpointClassesInOneDocument_AllFixed()
    {
        const string test = """
            using MinimalEndpoints.Annotations;

            namespace TestApp;

            [MapGet("/a")]
            public class {|MINEP001:EndpointA|}
            {
            }

            [MapGet("/b")]
            public class {|MINEP001:EndpointB|}
            {
            }
            """;

        const string fixedCode = """
            using MinimalEndpoints.Annotations;
            using Microsoft.AspNetCore.Http;

            namespace TestApp;

            [MapGet("/a")]
            public class EndpointA
            {
                public IResult Handle()
                {
                    return Results.Ok();
                }
            }

            [MapGet("/b")]
            public class EndpointB
            {
                public IResult Handle()
                {
                    return Results.Ok();
                }
            }
            """;

        await new MinEndpointsCodeFixTest
        {
            TestCode = test.ReplaceLineEndings(),
            FixedCode = fixedCode.ReplaceLineEndings(),
            CodeActionEquivalenceKey = "AddHandleMethod",
        }.RunAsync();
    }

    [Fact]
    public async Task FixAll_TwoEndpointClassesInSeparateDocuments_AllFixed()
    {
        const string testA = """
            using MinimalEndpoints.Annotations;

            namespace TestApp;

            [MapGet("/a")]
            public class {|MINEP001:EndpointA|}
            {
            }
            """;

        const string testB = """
            using MinimalEndpoints.Annotations;

            namespace TestApp;

            [MapGet("/b")]
            public class {|MINEP001:EndpointB|}
            {
            }
            """;

        const string fixedA = """
            using MinimalEndpoints.Annotations;
            using Microsoft.AspNetCore.Http;

            namespace TestApp;

            [MapGet("/a")]
            public class EndpointA
            {
                public IResult Handle()
                {
                    return Results.Ok();
                }
            }
            """;

        const string fixedB = """
            using MinimalEndpoints.Annotations;
            using Microsoft.AspNetCore.Http;

            namespace TestApp;

            [MapGet("/b")]
            public class EndpointB
            {
                public IResult Handle()
                {
                    return Results.Ok();
                }
            }
            """;

        var fixTest = new MinEndpointsCodeFixTest
        {
            CodeActionEquivalenceKey = "AddHandleMethod",
        };
        fixTest.TestState.Sources.Add(("/EndpointA.cs", testA.ReplaceLineEndings()));
        fixTest.TestState.Sources.Add(("/EndpointB.cs", testB.ReplaceLineEndings()));
        fixTest.FixedState.Sources.Add(("/EndpointA.cs", fixedA.ReplaceLineEndings()));
        fixTest.FixedState.Sources.Add(("/EndpointB.cs", fixedB.ReplaceLineEndings()));

        await fixTest.RunAsync();
    }

    // ---------------------------------------------------------------------------------------------
    // Regression tests for EntryPointCodeFixProvider defects: the async fix now emits a compilable,
    // non-async Task.FromResult body and adds `using System.Threading.Tasks;`, and a custom
    // `EntryPoint` is honored so applying the fix actually resolves MINEP001. (These were written
    // failing-first against the then-buggy provider.)
    // ---------------------------------------------------------------------------------------------

    [Fact] // Was bug (d): the fix emitted `async` with no `await` (CS1998) and never added
           // `using System.Threading.Tasks;` (CS0246 when the user file lacked it).
    public async Task AddHandleAsync_ProducesCompilableMethod_WithTasksUsing()
    {
        const string test = """
            using MinimalEndpoints.Annotations;

            namespace TestApp;

            [MapGet("/test")]
            public class {|MINEP001:TestEndpoint|}
            {
            }
            """;

        // A non-async body (no CS1998) that resolves `Task<IResult>` via an added
        // `using System.Threading.Tasks;` alongside `using Microsoft.AspNetCore.Http;`.
        const string fixedCode = """
            using MinimalEndpoints.Annotations;
            using Microsoft.AspNetCore.Http;
            using System.Threading.Tasks;

            namespace TestApp;

            [MapGet("/test")]
            public class TestEndpoint
            {
                public Task<IResult> HandleAsync()
                {
                    return Task.FromResult(Results.Ok());
                }
            }
            """;

        await new MinEndpointsCodeFixTest
        {
            TestCode = test.ReplaceLineEndings(),
            FixedCode = fixedCode.ReplaceLineEndings(),
            CodeActionEquivalenceKey = "AddHandleAsyncMethod",
            // Leave CompilerDiagnostics at the default (Errors only). Raising it to Warnings would
            // surface unrelated CS1591 doc-comment noise on the empty test class; the no-`async`
            // shape above is what pins the CS1998 fix.
        }.RunAsync();
    }

    [Fact] // Was bug (c): the fix ignored a custom `EntryPoint` and always emitted `Handle`/`HandleAsync`,
           // leaving MINEP001 unresolved (the analyzer was looking for `Execute`).
    public async Task CustomEntryPoint_FixGeneratesNamedMethod_ResolvesMinep001()
    {
        const string test = """
            using MinimalEndpoints.Annotations;

            namespace TestApp;

            [MapGet("/test", EntryPoint = "Execute")]
            public class {|MINEP001:TestEndpoint|}
            {
            }
            """;

        // The fix honors `EntryPoint = "Execute"` and generates a method with THAT name, resolving
        // MINEP001 (the "Add Handle method" action emits `Execute` here, not `Handle`).
        const string fixedCode = """
            using MinimalEndpoints.Annotations;
            using Microsoft.AspNetCore.Http;

            namespace TestApp;

            [MapGet("/test", EntryPoint = "Execute")]
            public class TestEndpoint
            {
                public IResult Execute()
                {
                    return Results.Ok();
                }
            }
            """;

        await new MinEndpointsCodeFixTest
        {
            TestCode = test.ReplaceLineEndings(),
            FixedCode = fixedCode.ReplaceLineEndings(),
            CodeActionEquivalenceKey = "AddHandleMethod",
        }.RunAsync();
    }

    [Fact] // Was bug (#7): EnsureUsingDirectives dereferenced UsingDirective.Name with the
           // null-forgiving operator, but a C# 12 alias to a non-name type (tuple/array) has a null
           // Name — so the fix threw an NRE and silently failed to apply on any file with such an alias.
    public async Task AddHandle_AliasToTupleUsingPresent_FixStillApplies()
    {
        const string test = """
            using MinimalEndpoints.Annotations;
            using Coord = (int X, int Y);

            namespace TestApp;

            [MapGet("/test")]
            public class {|MINEP001:TestEndpoint|}
            {
            }
            """;

        const string fixedCode = """
            using MinimalEndpoints.Annotations;
            using Coord = (int X, int Y);
            using Microsoft.AspNetCore.Http;

            namespace TestApp;

            [MapGet("/test")]
            public class TestEndpoint
            {
                public IResult Handle()
                {
                    return Results.Ok();
                }
            }
            """;

        await new MinEndpointsCodeFixTest
        {
            TestCode = test.ReplaceLineEndings(),
            FixedCode = fixedCode.ReplaceLineEndings(),
            CodeActionEquivalenceKey = "AddHandleMethod",
        }.RunAsync();
    }

    [Fact] // Was bug (#10): a custom EntryPoint that is a C# keyword was emitted verbatim as a method
           // identifier, producing uncompilable code that never resolved MINEP001.
    public async Task CustomEntryPoint_Keyword_EmitsVerbatimIdentifier_ResolvesMinep001()
    {
        const string test = """
            using MinimalEndpoints.Annotations;

            namespace TestApp;

            [MapGet("/test", EntryPoint = "class")]
            public class {|MINEP001:TestEndpoint|}
            {
            }
            """;

        const string fixedCode = """
            using MinimalEndpoints.Annotations;
            using Microsoft.AspNetCore.Http;

            namespace TestApp;

            [MapGet("/test", EntryPoint = "class")]
            public class TestEndpoint
            {
                public IResult @class()
                {
                    return Results.Ok();
                }
            }
            """;

        await new MinEndpointsCodeFixTest
        {
            TestCode = test.ReplaceLineEndings(),
            FixedCode = fixedCode.ReplaceLineEndings(),
            CodeActionEquivalenceKey = "AddHandleMethod",
        }.RunAsync();
    }
}
