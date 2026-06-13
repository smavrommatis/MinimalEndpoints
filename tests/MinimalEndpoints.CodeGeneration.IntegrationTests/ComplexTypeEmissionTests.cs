namespace MinimalEndpoints.CodeGeneration.IntegrationTests;

/// <summary>
/// End-to-end emission coverage for handler signatures whose parameter or return types stress
/// <c>TypeDefinition</c>'s rendering/simplification: tuples (named and unnamed), arrays (including
/// generic-of-array), and nullable reference types. Each case asserts the GENERATED source compiles
/// (GenerateCodeAndCompile validates it) and that the simplified type text is what we expect, so a
/// regression in type rendering surfaces here rather than only in the TypeDefinition unit tests.
/// </summary>
public class ComplexTypeEmissionTests
{
    private static string Generate(string code)
    {
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        // validateCompilation: true -> throws if the generated handler signature does not compile.
        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);
        return generatedCode;
    }

    [Fact]
    public void NamedTupleReturnType_EmitsValidSimplifiedTuple()
    {
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/point"")]
public class PointEndpoint
{
    public (int X, int Y) Handle() => (1, 2);
}";

        var generated = Generate(code);

        Assert.Contains("static (int X, int Y) Handler(", generated);
    }

    [Fact]
    public void TupleOfGenericReturnType_EmitsValidSimplifiedTuple()
    {
        // The regression case for the tuple element-name split: an element whose type is a
        // multi-argument generic ("Dictionary<string, int>") must render intact.
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/lookup"")]
public class LookupEndpoint
{
    public (Dictionary<string, int> Map, int Count) Handle() => (new(), 0);
}";

        var generated = Generate(code);

        Assert.Contains("(Dictionary<string, int> Map, int Count) Handler(", generated);
    }

    [Fact]
    public void ArrayParameter_EmitsValidArrayType()
    {
        var code = @"
namespace TestApp.Endpoints;

[MapPost(""/bulk"")]
public class BulkEndpoint
{
    public IResult Handle([FromBody] int[] ids) => Results.Ok(ids.Length);
}";

        var generated = Generate(code);

        Assert.Contains("int[] ids", generated);
    }

    [Fact]
    public void GenericOfArrayParameter_EmitsValidNestedType()
    {
        // The regression case for the array-before-generic branch order: List<int[]> must not be
        // sliced at the interior '['.
        var code = @"
namespace TestApp.Endpoints;

[MapPost(""/matrix"")]
public class MatrixEndpoint
{
    public IResult Handle([FromBody] List<int[]> rows) => Results.Ok(rows.Count);
}";

        var generated = Generate(code);

        Assert.Contains("List<int[]> rows", generated);
    }

    [Fact]
    public void NullableReferenceReturnType_PreservesAnnotation()
    {
        var code = @"
#nullable enable
namespace TestApp.Endpoints;

[MapGet(""/maybe"")]
public class MaybeEndpoint
{
    public string? Handle() => null;
}";

        var generated = Generate(code);

        Assert.Contains("static string? Handler(", generated);
    }

    [Fact]
    public void NullableReferenceInsideGenericReturnType_PreservesAnnotation()
    {
        var code = @"
#nullable enable
namespace TestApp.Endpoints;

[MapGet(""/names"")]
public class NamesEndpoint
{
    public Task<List<string?>> HandleAsync() => Task.FromResult(new List<string?>());
}";

        var generated = Generate(code);

        Assert.Contains("Task<List<string?>> Handler(", generated);
    }
}
