using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration.Tests.Models;

public class TypeDefinitionTests
{
    [Fact]
    public void ToDisplayString_SimplifiesSimpleType_WhenNamespaceInUsings()
    {
        // Arrange
        var code = @"
namespace MyNamespace
{
    public class MyClass { }
}";
        var typeSymbol = GetTypeSymbol(code, "MyNamespace.MyClass");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string> { "MyNamespace" };

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("MyClass", displayName);
        Assert.Equal("MyNamespace.MyClass", typeDef.FullName); // FullName remains unchanged
    }

    [Fact]
    public void ToDisplayString_KeepsFullName_WhenNamespaceNotInUsings()
    {
        // Arrange
        var code = @"
namespace MyNamespace
{
    public class MyClass { }
}";
        var typeSymbol = GetTypeSymbol(code, "MyNamespace.MyClass");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("MyNamespace.MyClass", displayName);
    }

    [Fact]
    public void ToDisplayString_SimplifiesGenericType_WhenNamespaceInUsings()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public Task<int> TaskProperty { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "TaskProperty");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string> { "System.Threading.Tasks" };

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("Task<int>", displayName);
        Assert.Equal("System.Threading.Tasks.Task<int>", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_KeepsFullNameForGeneric_WhenNamespaceNotInUsings()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public Task<int> TaskProperty { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "TaskProperty");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("System.Threading.Tasks.Task<int>", displayName);
    }

    [Fact]
    public void ToDisplayString_SimplifiesNestedGenerics_WithPartialUsings()
    {
        // Arrange
        var code = @"
using System.Collections.Generic;
using System.Threading.Tasks;

public class Test
{
    public Task<Dictionary<string, List<int>>> ComplexProperty { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "ComplexProperty");
        var typeDef = new TypeDefinition(typeSymbol);

        // Only Task namespace in usings, not Dictionary
        var usings = new HashSet<string> { "System.Threading.Tasks" };

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("Task<System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<int>>>", displayName);
    }

    [Fact]
    public void ToDisplayString_SimplifiesNestedGenerics_WithAllUsings()
    {
        // Arrange
        var code = @"
using System.Collections.Generic;
using System.Threading.Tasks;

public class Test
{
    public Task<Dictionary<string, List<int>>> ComplexProperty { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "ComplexProperty");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string> { "System.Threading.Tasks", "System.Collections.Generic" };

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("Task<Dictionary<string, List<int>>>", displayName);
    }

    [Fact]
    public void ToDisplayString_HandlesArrays_WithUsings()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public Task<int>[] TaskArray { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "TaskArray");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string> { "System.Threading.Tasks" };

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("Task<int>[]", displayName);
        Assert.Equal("System.Threading.Tasks.Task<int>[]", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableTypes_WithUsings()
    {
        // Arrange
        var code = @"
public class Test
{
    public int? NullableInt { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableInt");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("int?", displayName);
        Assert.Equal("int?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_BuiltInTypes_AlwaysSimplified()
    {
        // Arrange
        var code = @"
public class Test
{
    public int IntProperty { get; set; }
    public string StringProperty { get; set; }
}";
        var compilation = new CompilationBuilder(code).Build();
        var testType = compilation.GetTypeByMetadataName("Test");
        var intProp = testType!.GetMembers("IntProperty").First() as IPropertySymbol;
        var stringProp = testType!.GetMembers("StringProperty").First() as IPropertySymbol;

        var intTypeDef = new TypeDefinition(intProp!.Type);
        var stringTypeDef = new TypeDefinition(stringProp!.Type);
        var usings = new HashSet<string>(); // Empty usings

        // Act
        var intDisplay = intTypeDef.ToDisplayString(usings);
        var stringDisplay = stringTypeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("int", intDisplay);
        Assert.Equal("string", stringDisplay);
    }

    [Fact]
    public void ToDisplayString_CustomTypeInGeneric_WithMixedUsings()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

namespace MyNamespace
{
    public class MyCustomClass { }

    public class Test
    {
        public Task<MyCustomClass> TaskProperty { get; set; }
    }
}";
        var typeSymbol = GetPropertyType(code, "MyNamespace.Test", "TaskProperty");
        var typeDef = new TypeDefinition(typeSymbol);

        // Only Task in usings, not MyNamespace
        var usings = new HashSet<string> { "System.Threading.Tasks" };

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("Task<MyNamespace.MyCustomClass>", displayName);
    }

    [Fact]
    public void ToDisplayString_CustomTypeInGeneric_WithAllUsings()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

namespace MyNamespace
{
    public class MyCustomClass { }

    public class Test
    {
        public Task<MyCustomClass> TaskProperty { get; set; }
    }
}";
        var typeSymbol = GetPropertyType(code, "MyNamespace.Test", "TaskProperty");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string> { "System.Threading.Tasks", "MyNamespace" };

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("Task<MyCustomClass>", displayName);
    }

    private static ITypeSymbol GetTypeSymbol(string code, string typeName)
    {
        var compilation = new CompilationBuilder(code).Build();
        return compilation.GetTypeByMetadataName(typeName)!;
    }

    private static ITypeSymbol GetPropertyType(string code, string typeName, string propertyName)
    {
        var compilation = new CompilationBuilder(code).Build();
        var type = compilation.GetTypeByMetadataName(typeName);
        var property = type!.GetMembers(propertyName).First() as IPropertySymbol;
        return property!.Type;
    }
}

