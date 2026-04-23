using ReadyM.Api.Generators.TypeTranslation.Model;
using ReadyM.Api.Generators.TypeTranslation.Rendering;
using Xunit;

namespace ReadyM.Api.Generators.Tests.TypeTranslation;

public sealed class CppTypeRendererTests
{
    [Fact]
    public void Render_ShouldRenderQualifiedGenericType()
    {
        ITypeName typeName = new GenericInstanceName(
            new QualifiedName(
                new TypeName("A"),
                new TypeName("B")),
            [new TypeName("int"), new Numeric(8)]);

        var renderer = new CppTypeRenderer();

        var result = renderer.Render(typeName);

        Assert.Equal("A::B<int32_t, 8>", result);
    }

    [Fact]
    public void Render_ShouldRenderSingleName()
    {
        var renderer = new CppTypeRenderer();

        var result = renderer.Render(new TypeName("MyType"));

        Assert.Equal("MyType", result);
    }

    [Fact]
    public void Render_ShouldRenderTypeParameter()
    {
        var renderer = new CppTypeRenderer();

        var result = renderer.Render(new TypeParam("T"));

        Assert.Equal("T", result);
    }

    [Fact]
    public void Render_ShouldRenderNumericConstant()
    {
        var renderer = new CppTypeRenderer();

        var result = renderer.Render(new Numeric(42));

        Assert.Equal("42", result);
    }

    [Fact]
    public void Render_ShouldRenderNestedGenericArguments()
    {
        var renderer = new CppTypeRenderer();
        var input = TypeNameFactory.Generic(
            TypeNameFactory.Qualified("Outer"),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Inner"),
                TypeNameFactory.Name("int"),
                TypeNameFactory.Number(8)));

        var result = renderer.Render(input);

        Assert.Equal("Outer<Inner<int32_t, 8>>", result);
    }

    [Fact]
    public void Render_ShouldRenderSystemQualifiedSpecialType()
    {
        var renderer = new CppTypeRenderer();
        var input = TypeNameFactory.Qualified("System", "Int32");

        var result = renderer.Render(input);

        Assert.Equal("int32_t", result);
    }

    [Fact]
    public void Render_ShouldRenderSystemQualifiedStringAsInteropString()
    {
        var renderer = new CppTypeRenderer();
        var input = TypeNameFactory.Qualified("System", "String");

        var result = renderer.Render(input);

        Assert.Equal("Interop::String", result);
    }
}
