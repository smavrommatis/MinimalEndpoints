using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MinimalEndpoints.Analyzers.Models;

namespace MinimalEndpoints.Analyzers.Tests.Models;

public class AttributeDefinitionTests
{
    [Fact]
    public void AttributeDefinition_HandlesSimpleAttribute()
    {
        // Arrange
        var code = @"
using System.ComponentModel;

public class TestClass
{
    public void Method([Description("""")] int parameter) { }
}";
        var compilation = new CompilationBuilder(code)
            .Build();
        var attributeData = GetParameterAttribute(compilation, "TestClass", "Method", "parameter");

        // Act
        var attrDef = AttributeDefinition.FromAttributeData(attributeData);
        var displayString = attrDef.ToDisplayString(new HashSet<string>());

        // Assert
        Assert.Equal("[System.ComponentModel.Description(\"\")]", displayString);
    }

    [Fact]
    public void AttributeDefinition_RemovesAttributeSuffix()
    {
        // Arrange
        var code = @"
using System.ComponentModel;

public class TestClass
{
    public void Method([Description("""")] int parameter) { }
}";
        var compilation = new CompilationBuilder(code)
            .Build();
        var attributeData = GetParameterAttribute(compilation, "TestClass", "Method", "parameter");
        var usings = new HashSet<string> { "System.ComponentModel" };

        // Act
        var attrDef = AttributeDefinition.FromAttributeData(attributeData);
        var displayString = attrDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("[Description(\"\")]", displayString);
    }

    [Fact]
    public void AttributeDefinition_HandlesStringArgument()
    {
        // Arrange
        var code = @"
using System.ComponentModel;

public class TestClass
{
    public void Method([Description(""This is a description"")] int parameter) { }
}";
        var compilation = new CompilationBuilder(code)
            .Build();
        var attributeData = GetParameterAttribute(compilation, "TestClass", "Method", "parameter");
        var usings = new HashSet<string> { "System.ComponentModel" };

        // Act
        var attrDef = AttributeDefinition.FromAttributeData(attributeData);
        var displayString = attrDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("[Description(\"This is a description\")]", displayString);
    }

    [Fact]
    public void AttributeDefinition_HandlesBooleanArgument()
    {
        // Arrange
        var code = @"
using System.ComponentModel;

public class TestClass
{
    public void Method([DefaultValue(true)] bool parameter) { }
}";
        var compilation = new CompilationBuilder(code)
            .Build();
        var attributeData = GetParameterAttribute(compilation, "TestClass", "Method", "parameter");
        var usings = new HashSet<string> { "System.ComponentModel" };

        // Act
        var attrDef = AttributeDefinition.FromAttributeData(attributeData);
        var displayString = attrDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("[DefaultValue(true)]", displayString);
    }

    [Fact]
    public void AttributeDefinition_HandlesNamedArguments()
    {
        // Arrange
        var code = @"
using System.ComponentModel.DataAnnotations;

public class TestClass
{
    public void Method([StringLength(100, MinimumLength = 10)] string parameter) { }
}";
        var compilation = new CompilationBuilder(code)
            .Build();
        var attributeData = GetParameterAttribute(compilation, "TestClass", "Method", "parameter");
        var usings = new HashSet<string> { "System.ComponentModel.DataAnnotations" };

        // Act
        var attrDef = AttributeDefinition.FromAttributeData(attributeData);
        var displayString = attrDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("[StringLength(100, MinimumLength = 10)]", displayString);
    }

    [Fact]
    public void AttributeDefinition_HandlesFromBodyAttribute()
    {
        // Arrange
        var code = @"
using Microsoft.AspNetCore.Mvc;

public class TestClass
{
    public void Method([FromBody] string parameter) { }
}";
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var attributeData = GetParameterAttribute(compilation, "TestClass", "Method", "parameter");
        var usings = new HashSet<string> { "Microsoft.AspNetCore.Mvc" };

        // Act
        var attrDef = AttributeDefinition.FromAttributeData(attributeData);
        var displayString = attrDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("[FromBody]", displayString);
    }

    [Fact]
    public void AttributeDefinition_HandlesFromServicesAttribute()
    {
        // Arrange
        var code = @"
using Microsoft.AspNetCore.Mvc;

public class TestClass
{
    public void Method([FromServices] string parameter) { }
}";
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var attributeData = GetParameterAttribute(compilation, "TestClass", "Method", "parameter");
        var usings = new HashSet<string> { "Microsoft.AspNetCore.Mvc" };

        // Act
        var attrDef = AttributeDefinition.FromAttributeData(attributeData);
        var displayString = attrDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("[FromServices]", displayString);
    }

    [Fact]
    public void AttributeDefinition_HandlesMultipleArguments()
    {
        // Arrange
        var code = @"
using System.ComponentModel.DataAnnotations;

public class TestClass
{
    public void Method([Range(1, 100)] int parameter) { }
}";
        var compilation = new CompilationBuilder(code)
            .Build();
        var attributeData = GetParameterAttribute(compilation, "TestClass", "Method", "parameter");
        var usings = new HashSet<string> { "System.ComponentModel.DataAnnotations" };

        // Act
        var attrDef = AttributeDefinition.FromAttributeData(attributeData);
        var displayString = attrDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("[Range(1, 100)]", displayString);
    }

    [Fact]
    public void AttributeDefinition_HandlesEnumArgument()
    {
        // Arrange
        var code = @"
using System.ComponentModel;

public class TestClass
{
    public void Method([DefaultValue(System.StringComparison.OrdinalIgnoreCase)] string parameter) { }
}";
        var compilation = new CompilationBuilder(code)
            .Build();
        var attributeData = GetParameterAttribute(compilation, "TestClass", "Method", "parameter");
        var usings = new HashSet<string> { "System.ComponentModel" };

        // Act
        var attrDef = AttributeDefinition.FromAttributeData(attributeData);
        var displayString = attrDef.ToDisplayString(usings);

        // Assert
        Assert.Contains("DefaultValue", displayString);
        Assert.Contains("StringComparison", displayString);
    }

    [Fact]
    public void AttributeDefinition_SimplifiesWithUsings()
    {
        // Arrange
        var code = @"
using Microsoft.AspNetCore.Mvc;

public class TestClass
{
    public void Method([FromBody] string parameter) { }
}";
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var attributeData = GetParameterAttribute(compilation, "TestClass", "Method", "parameter");

        // Act without usings
        var attrDefNoUsings = AttributeDefinition.FromAttributeData(attributeData);
        var displayNoUsings = attrDefNoUsings.ToDisplayString(new HashSet<string>());

        // Act with usings
        var attrDefWithUsings = AttributeDefinition.FromAttributeData(attributeData);
        var displayWithUsings = attrDefWithUsings.ToDisplayString(new HashSet<string> { "Microsoft.AspNetCore.Mvc" });

        // Assert
        Assert.Equal("[Microsoft.AspNetCore.Mvc.FromBody]", displayNoUsings);
        Assert.Equal("[FromBody]", displayWithUsings);
    }

    [Fact]
    public void AttributeDefinition_HandlesNullValue()
    {
        // Arrange
        var code = @"
using System.ComponentModel;

public class TestClass
{
    public void Method([DefaultValue(null)] string parameter) { }
}";
        var compilation = new CompilationBuilder(code).Build();
        var attributeData = GetParameterAttribute(compilation, "TestClass", "Method", "parameter");
        var usings = new HashSet<string> { "System.ComponentModel" };

        // Act
        var attrDef = AttributeDefinition.FromAttributeData(attributeData);
        var displayString = attrDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("[DefaultValue(null)]", displayString);
    }

    [Fact]
    public void AttributeDefinition_HandlesNumericTypes()
    {
        // Arrange
        var code = @"
using System.ComponentModel.DataAnnotations;

public class TestClass
{
    public void Method([Range(1.5, 99.9)] double parameter) { }
}";
        var compilation = new CompilationBuilder(code).Build();
        var attributeData = GetParameterAttribute(compilation, "TestClass", "Method", "parameter");
        var usings = new HashSet<string> { "System.ComponentModel.DataAnnotations" };

        // Act
        var attrDef = AttributeDefinition.FromAttributeData(attributeData);
        var displayString = attrDef.ToDisplayString(usings);

        // Assert
        Assert.Contains("Range", displayString);
        Assert.Contains("1.5", displayString);
        Assert.Contains("99.9", displayString);
    }

    private static AttributeData GetParameterAttribute(CSharpCompilation compilation, string typeName, string methodName, string parameterName)
    {
        var type = compilation.GetTypeByMetadataName(typeName);
        var method = type!.GetMembers(methodName).OfType<IMethodSymbol>().First();
        var parameter = method.Parameters.First(p => p.Name == parameterName);
        return parameter.GetAttributes().First();
    }
}

