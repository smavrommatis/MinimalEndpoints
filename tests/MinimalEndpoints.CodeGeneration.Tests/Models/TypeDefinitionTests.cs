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

    [Fact]
    public void BuildFullTypeName_SpecialTypeWithoutKeyword_RendersQualifiedName_NotObject()
    {
        // System.IDisposable has SpecialType != None but no C# keyword alias; it must render as its
        // qualified name, not collapse to "object" (which produced non-compiling handler signatures).
        var code = @"
using System;

public class Test
{
    public IDisposable Disposable { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "Disposable");
        var typeDef = new TypeDefinition(typeSymbol);

        Assert.Equal("System.IDisposable", typeDef.FullName);
        Assert.Equal("IDisposable", typeDef.ToDisplayString(new HashSet<string> { "System" }));
    }

    [Fact]
    public void BuildFullTypeName_NonGenericIEnumerable_RendersQualifiedName_NotObject()
    {
        var code = @"
public class Test
{
    public System.Collections.IEnumerable Sequence { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "Sequence");
        var typeDef = new TypeDefinition(typeSymbol);

        Assert.Equal("System.Collections.IEnumerable", typeDef.FullName);
    }

    [Fact]
    public void BuildFullTypeName_GenericNestedInGeneric_IncludesContainingType()
    {
        // Outer<int>.Inner<string> used to render as "MyNs.Inner<string>" — the container was dropped.
        var code = @"
namespace MyNs
{
    public class Outer<T>
    {
        public class Inner<U> { }
    }

    public class Test
    {
        public Outer<int>.Inner<string> Prop { get; set; }
    }
}";
        var typeSymbol = GetPropertyType(code, "MyNs.Test", "Prop");
        var typeDef = new TypeDefinition(typeSymbol);

        Assert.Equal("MyNs.Outer<int>.Inner<string>", typeDef.FullName);
    }

    [Fact]
    public void BuildFullTypeName_NonGenericNestedInGeneric_IncludesContainingType()
    {
        var code = @"
namespace MyNs
{
    public class Outer<T>
    {
        public class Inner { }
    }

    public class Test
    {
        public Outer<int>.Inner Prop { get; set; }
    }
}";
        var typeSymbol = GetPropertyType(code, "MyNs.Test", "Prop");
        var typeDef = new TypeDefinition(typeSymbol);

        Assert.Equal("MyNs.Outer<int>.Inner", typeDef.FullName);
    }

    [Fact]
    public void BuildFullTypeName_GenericNestedInNonGeneric_IncludesContainingType()
    {
        var code = @"
namespace MyNs
{
    public class Outer
    {
        public class Inner<T> { }
    }

    public class Test
    {
        public Outer.Inner<int> Prop { get; set; }
    }
}";
        var typeSymbol = GetPropertyType(code, "MyNs.Test", "Prop");
        var typeDef = new TypeDefinition(typeSymbol);

        Assert.Equal("MyNs.Outer.Inner<int>", typeDef.FullName);
    }

    [Fact]
    public void ToDisplayString_NestedTypeOnClosedGeneric_RendersValidName_WhenNamespaceInUsings()
    {
        // Regression: SimplifyGenericType assumed the closing '>' was the final character, so a
        // nested type on a CLOSED generic outer (Outer<int>.Inner) sliced past the matching '>'
        // and emitted invalid C# ("Outer<int>.Inne>"). The simplified name must stay valid.
        var code = @"
namespace MyNs
{
    public class Outer<T>
    {
        public class Inner { }
    }

    public class Test
    {
        public Outer<int>.Inner Prop { get; set; }
    }
}";
        var typeSymbol = GetPropertyType(code, "MyNs.Test", "Prop");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string> { "MyNs" };

        Assert.Equal("Outer<int>.Inner", typeDef.ToDisplayString(usings));
    }

    [Fact]
    public void ToDisplayString_NestedTypeOnClosedGeneric_KeepsFullName_WhenNamespaceNotInUsings()
    {
        var code = @"
namespace MyNs
{
    public class Outer<T>
    {
        public class Inner { }
    }

    public class Test
    {
        public Outer<int>.Inner Prop { get; set; }
    }
}";
        var typeSymbol = GetPropertyType(code, "MyNs.Test", "Prop");
        var typeDef = new TypeDefinition(typeSymbol);

        Assert.Equal("MyNs.Outer<int>.Inner", typeDef.ToDisplayString(new HashSet<string>()));
    }

    [Fact]
    public void ToDisplayString_GenericNestedTypeOnClosedGeneric_RendersValidName()
    {
        // The trailing nested segment itself carries generic arguments (Inner<string>); both the
        // outer closed generic and the nested generic must survive simplification.
        var code = @"
namespace MyNs
{
    public class Outer<T>
    {
        public class Inner<U> { }
    }

    public class Test
    {
        public Outer<int>.Inner<string> Prop { get; set; }
    }
}";
        var typeSymbol = GetPropertyType(code, "MyNs.Test", "Prop");
        var typeDef = new TypeDefinition(typeSymbol);
        var usings = new HashSet<string> { "MyNs" };

        Assert.Equal("Outer<int>.Inner<string>", typeDef.ToDisplayString(usings));
    }

    [Fact]
    public void ToDisplayString_UnnamedTupleWithMultiArgGenericElement_RendersValidName()
    {
        // Regression: SimplifyTupleType split each element on its FIRST space to detect an element
        // name. For a tuple element whose type is a multi-arg generic, that first space is the one
        // inside "<string, int>", so the type was mangled into "(Dictionary<string> int>, int)".
        var code = @"
using System.Collections.Generic;

public class Test
{
    public (Dictionary<string, int>, int) Prop { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "Prop");
        var typeDef = new TypeDefinition(typeSymbol);

        Assert.Equal("(System.Collections.Generic.Dictionary<string, int>, int)", typeDef.FullName);
        Assert.Equal(
            "(Dictionary<string, int>, int)",
            typeDef.ToDisplayString(new HashSet<string> { "System.Collections.Generic" }));
    }

    [Fact]
    public void ToDisplayString_NamedTupleWithMultiArgGenericElement_RendersValidName()
    {
        var code = @"
using System.Collections.Generic;

public class Test
{
    public (Dictionary<string, int> map, int n) Prop { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "Prop");
        var typeDef = new TypeDefinition(typeSymbol);

        Assert.Equal("(System.Collections.Generic.Dictionary<string, int> map, int n)", typeDef.FullName);
        Assert.Equal(
            "(Dictionary<string, int> map, int n)",
            typeDef.ToDisplayString(new HashSet<string> { "System.Collections.Generic" }));
    }

    [Fact]
    public void ToDisplayString_GenericOfMultidimArray_RendersValidName()
    {
        // Regression: SimplifyTypeName tested the array branch (Contains '[') BEFORE the generic
        // branch (Contains '<'), so List<int[,]> sliced at the interior '[' and emitted invalid C#.
        var code = @"
using System.Collections.Generic;

public class Test
{
    public List<int[,]> Prop { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "Prop");
        var typeDef = new TypeDefinition(typeSymbol);

        Assert.Equal("System.Collections.Generic.List<int[,]>", typeDef.FullName);
        Assert.Equal(
            "List<int[,]>",
            typeDef.ToDisplayString(new HashSet<string> { "System.Collections.Generic" }));
    }

    [Fact]
    public void BuildFullTypeName_JaggedMultidimArray_PreservesRankOrder()
    {
        // Regression: building the name element-first reversed the rank order to "int[,][]", which is
        // a DIFFERENT CLR type than the user's "int[][,]" and would not compile against their method.
        var code = @"
public class Test
{
    public int[][,] Prop { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "Prop");
        var typeDef = new TypeDefinition(typeSymbol);

        Assert.Equal("int[][,]", typeDef.FullName);
    }

    [Fact]
    public void BuildFullTypeName_TopLevelNullableReferenceType_PreservesAnnotation()
    {
        // Regression: BuildFullTypeName only honored System.Nullable<T> (value types); the nullable
        // annotation on a reference type was dropped, so a "string?" return type rendered as "string"
        // and emitted a nullability-mismatched handler under #nullable enable.
        var code = @"
#nullable enable
public class Test
{
    public string? Prop { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "Prop");
        var typeDef = new TypeDefinition(typeSymbol);

        Assert.Equal("string?", typeDef.FullName);
    }

    [Fact]
    public void BuildFullTypeName_NestedNullableReferenceType_PreservesAnnotation()
    {
        var code = @"
#nullable enable
using System.Collections.Generic;

public class Test
{
    public List<string?> Prop { get; set; }
}";
        var typeSymbol = GetPropertyType(code, "Test", "Prop");
        var typeDef = new TypeDefinition(typeSymbol);

        Assert.Equal("System.Collections.Generic.List<string?>", typeDef.FullName);
        Assert.Equal(
            "List<string?>",
            typeDef.ToDisplayString(new HashSet<string> { "System.Collections.Generic" }));
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

