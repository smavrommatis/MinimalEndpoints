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
    public void ToDisplayString_HandlesNullableInt_WithoutUsings()
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
    public void ToDisplayString_HandlesNullableDateTime_WithoutUsings()
    {
        // Arrange
        var code = @"
public class Test
{
    public DateTime? NullableDateTime { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableDateTime");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("System.DateTime?", displayName);
        Assert.Equal("System.DateTime?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableDateTime_WithSystemUsing()
    {
        // Arrange
        var code = @"
public class Test
{
    public DateTime? NullableDateTime { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableDateTime");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string> { "System" };

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("DateTime?", displayName);
        Assert.Equal("System.DateTime?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableBool_WithoutUsings()
    {
        // Arrange
        var code = @"
public class Test
{
    public bool? NullableBool { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableBool");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("bool?", displayName);
        Assert.Equal("bool?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableLong_WithoutUsings()
    {
        // Arrange
        var code = @"
public class Test
{
    public long? NullableLong { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableLong");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("long?", displayName);
        Assert.Equal("long?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableDecimal_WithoutUsings()
    {
        // Arrange
        var code = @"
public class Test
{
    public decimal? NullableDecimal { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableDecimal");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("decimal?", displayName);
        Assert.Equal("decimal?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableDouble_WithoutUsings()
    {
        // Arrange
        var code = @"
public class Test
{
    public double? NullableDouble { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableDouble");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("double?", displayName);
        Assert.Equal("double?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableFloat_WithoutUsings()
    {
        // Arrange
        var code = @"
public class Test
{
    public float? NullableFloat { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableFloat");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("float?", displayName);
        Assert.Equal("float?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableByte_WithoutUsings()
    {
        // Arrange
        var code = @"
public class Test
{
    public byte? NullableByte { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableByte");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("byte?", displayName);
        Assert.Equal("byte?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableShort_WithoutUsings()
    {
        // Arrange
        var code = @"
public class Test
{
    public short? NullableShort { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableShort");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("short?", displayName);
        Assert.Equal("short?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableGuid_WithoutUsings()
    {
        // Arrange
        var code = @"
public class Test
{
    public Guid? NullableGuid { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableGuid");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("System.Guid?", displayName);
        Assert.Equal("System.Guid?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableGuid_WithSystemUsing()
    {
        // Arrange
        var code = @"
public class Test
{
    public Guid? NullableGuid { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableGuid");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string> { "System" };

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("Guid?", displayName);
        Assert.Equal("System.Guid?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableChar_WithoutUsings()
    {
        // Arrange
        var code = @"
public class Test
{
    public char? NullableChar { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableChar");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("char?", displayName);
        Assert.Equal("char?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableDateOnly_WithoutUsings()
    {
        // Arrange
        var code = @"
public class Test
{
    public DateOnly? NullableDateOnly { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableDateOnly");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("System.DateOnly?", displayName);
        Assert.Equal("System.DateOnly?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableDateOnly_WithSystemUsing()
    {
        // Arrange
        var code = @"
public class Test
{
    public DateOnly? NullableDateOnly { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableDateOnly");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string> { "System" };

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("DateOnly?", displayName);
        Assert.Equal("System.DateOnly?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableTimeOnly_WithoutUsings()
    {
        // Arrange
        var code = @"
public class Test
{
    public TimeOnly? NullableTimeOnly { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableTimeOnly");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("System.TimeOnly?", displayName);
        Assert.Equal("System.TimeOnly?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableTimeOnly_WithSystemUsing()
    {
        // Arrange
        var code = @"
public class Test
{
    public TimeOnly? NullableTimeOnly { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableTimeOnly");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string> { "System" };

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("TimeOnly?", displayName);
        Assert.Equal("System.TimeOnly?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableCustomStruct_WithoutUsings()
    {
        // Arrange
        var code = @"
namespace MyNamespace
{
    public struct MyStruct
    {
        public int Value { get; set; }
    }

    public class Test
    {
        public MyStruct? NullableStruct { get; set; }
    }
}";
        var typeSymbol = GetPropertyType(code, "MyNamespace.Test", "NullableStruct");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string>();

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("MyNamespace.MyStruct?", displayName);
        Assert.Equal("MyNamespace.MyStruct?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableCustomStruct_WithNamespaceUsing()
    {
        // Arrange
        var code = @"
namespace MyNamespace
{
    public struct MyStruct
    {
        public int Value { get; set; }
    }

    public class Test
    {
        public MyStruct? NullableStruct { get; set; }
    }
}";
        var typeSymbol = GetPropertyType(code, "MyNamespace.Test", "NullableStruct");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string> { "MyNamespace" };

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("MyStruct?", displayName);
        Assert.Equal("MyNamespace.MyStruct?", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableInGenericType_WithUsings()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public Task<int?> NullableTaskResult { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableTaskResult");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string> { "System.Threading.Tasks" };

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("Task<int?>", displayName);
        Assert.Equal("System.Threading.Tasks.Task<int?>", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_HandlesNullableDateTimeInGenericType_WithUsings()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public Task<DateTime?> NullableDateTimeTaskResult { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "NullableDateTimeTaskResult");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string> { "System.Threading.Tasks", "System" };

        // Act
        var displayName = typeDef.ToDisplayString(usings);

        // Assert
        Assert.Equal("Task<DateTime?>", displayName);
        Assert.Equal("System.Threading.Tasks.Task<System.DateTime?>", typeDef.FullName);
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

