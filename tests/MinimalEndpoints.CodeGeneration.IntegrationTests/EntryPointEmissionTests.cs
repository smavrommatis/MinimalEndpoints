namespace MinimalEndpoints.CodeGeneration.IntegrationTests;

/// <summary>
/// Emission coverage for the handler call site that invokes the endpoint's entry-point method.
/// </summary>
public class EntryPointEmissionTests
{
    private static string Generate(string code)
    {
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        // validateCompilation: true -> throws if the generated source does not compile.
        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);
        return generatedCode;
    }

    [Fact]
    public void EntryPoint_KeywordMethodName_EmitsEscapedIdentifier_AndCompiles()
    {
        // A custom entry-point whose name is a reserved C# keyword must be emitted as a verbatim
        // identifier (@class) at the handler call site; otherwise the generated "instance.class()"
        // is not valid C# and the generated source fails to compile.
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/widget"", EntryPoint = ""class"")]
public class WidgetEndpoint
{
    public IResult @class() => Results.Ok();
}";

        var generated = Generate(code);

        Assert.Contains("endpointInstance.@class(", generated);
    }
}
