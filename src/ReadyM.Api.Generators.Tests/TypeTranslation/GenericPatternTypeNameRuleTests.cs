using ReadyM.Api.Generators.TypeTranslation.Model;
using ReadyM.Api.Generators.TypeTranslation.Rendering;
using ReadyM.Api.Generators.TypeTranslation.Rules;
using Xunit;

namespace ReadyM.Api.Generators.Tests.TypeTranslation;

public sealed class GenericPatternTypeNameRuleTests
{
    private static string Render(ITypeName typeName) => new CppTypeRenderer().Render(typeName);
    [Fact]
    public void Translate_ShouldReplaceMatchedTypeArguments()
    {
        var rule = new GenericPatternTypeNameRule(
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Name", "Space", "SomeType"),
                TypeNameFactory.Param("X"),
                TypeNameFactory.Param("Y")),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Space", "OtherType"),
                TypeNameFactory.Param("X"),
                TypeNameFactory.Param("Y")));

        var input = TypeNameFactory.Generic(
            TypeNameFactory.Qualified("Name", "Space", "SomeType"),
            TypeNameFactory.Qualified("System", "String"),
            TypeNameFactory.Qualified("System", "Int32"));

        var success = rule.TryTranslate(input, out var output);
        var renderedInput = Render(input);
        var rendered = Render(output);

        Assert.Equal("Name::Space::SomeType<Interop::String, int32_t>", renderedInput);
        Assert.True(success);
        Assert.Equal("Space::OtherType<Interop::String, int32_t>", rendered);
    }

    [Fact]
    public void Translate_ShouldReturnFalse_WhenGenericDefinitionDoesNotMatch()
    {
        var rule = new GenericPatternTypeNameRule(
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Name", "Space", "SomeType"),
                TypeNameFactory.Param("X"),
                TypeNameFactory.Param("Y")),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Space", "OtherType"),
                TypeNameFactory.Param("X"),
                TypeNameFactory.Param("Y")));

        var input = TypeNameFactory.Generic(
            TypeNameFactory.Qualified("Name", "Space", "DifferentType"),
            TypeNameFactory.Name("int"),
            TypeNameFactory.Name("string"));

        var success = rule.TryTranslate(input, out var output);
        var renderedInput = Render(input);

        Assert.Equal("Name::Space::DifferentType<int32_t, Interop::String>", renderedInput);
        Assert.False(success);
        Assert.Same(input, output);
    }

    [Fact]
    public void Translate_ShouldReturnFalse_WhenGenericArityDoesNotMatch()
    {
        var rule = new GenericPatternTypeNameRule(
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Pair"),
                TypeNameFactory.Param("X"),
                TypeNameFactory.Param("Y")),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("MappedPair"),
                TypeNameFactory.Param("X"),
                TypeNameFactory.Param("Y")));

        var input = TypeNameFactory.Generic(
            TypeNameFactory.Qualified("Pair"),
            TypeNameFactory.Name("int"));

        var success = rule.TryTranslate(input, out var output);
        var renderedInput = Render(input);

        Assert.Equal("Pair<int32_t>", renderedInput);
        Assert.False(success);
        Assert.Same(input, output);
    }

    [Fact]
    public void Translate_ShouldRequireRepeatedBindingToMatchSameType()
    {
        var rule = new GenericPatternTypeNameRule(
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Pair"),
                TypeNameFactory.Param("X"),
                TypeNameFactory.Param("X")),
            TypeNameFactory.Param("X"));

        var input = TypeNameFactory.Generic(
            TypeNameFactory.Qualified("Pair"),
            TypeNameFactory.Name("int"),
            TypeNameFactory.Name("string"));

        var success = rule.TryTranslate(input, out var output);
        var renderedInput = Render(input);

        Assert.Equal("Pair<int32_t, Interop::String>", renderedInput);
        Assert.False(success);
        Assert.Same(input, output);
    }

    [Fact]
    public void Translate_ShouldSubstituteRepeatedBindingsIntoReplacement()
    {
        var rule = new GenericPatternTypeNameRule(
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Wrapper"),
                TypeNameFactory.Param("T")),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Pair"),
                TypeNameFactory.Param("T"),
                TypeNameFactory.Param("T")));

        var input = TypeNameFactory.Generic(
            TypeNameFactory.Qualified("Wrapper"),
            TypeNameFactory.Qualified("System", "String"));

        var success = rule.TryTranslate(input, out var output);
        var renderedInput = Render(input);
        var rendered = Render(output);

        Assert.Equal("Wrapper<Interop::String>", renderedInput);
        Assert.True(success);
        Assert.Equal("Pair<Interop::String, Interop::String>", rendered);
    }

    [Fact]
    public void Translate_ShouldMatchNestedGenericBindingAndReuseIt()
    {
        var rule = new GenericPatternTypeNameRule(
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Outer"),
                TypeNameFactory.Generic(
                    TypeNameFactory.Qualified("Inner"),
                    TypeNameFactory.Param("T")),
                TypeNameFactory.Param("T")),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Result"),
                TypeNameFactory.Param("T")));

        var input = TypeNameFactory.Generic(
            TypeNameFactory.Qualified("Outer"),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Inner"),
                TypeNameFactory.Qualified("System", "Int32")),
            TypeNameFactory.Qualified("System", "Int32"));

        var success = rule.TryTranslate(input, out var output);
        var renderedInput = Render(input);
        var rendered = Render(output);

        Assert.Equal("Outer<Inner<int32_t>, int32_t>", renderedInput);
        Assert.True(success);
        Assert.Equal("Result<int32_t>", rendered);
    }

    [Fact]
    public void Translate_ShouldReturnFalse_WhenNestedGenericBindingConflicts()
    {
        var rule = new GenericPatternTypeNameRule(
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Outer"),
                TypeNameFactory.Generic(
                    TypeNameFactory.Qualified("Inner"),
                    TypeNameFactory.Param("T")),
                TypeNameFactory.Param("T")),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Result"),
                TypeNameFactory.Param("T")));

        var input = TypeNameFactory.Generic(
            TypeNameFactory.Qualified("Outer"),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Inner"),
                TypeNameFactory.Qualified("System", "Int32")),
            TypeNameFactory.Qualified("System", "String"));

        var success = rule.TryTranslate(input, out var output);
        var renderedInput = Render(input);

        Assert.Equal("Outer<Inner<int32_t>, Interop::String>", renderedInput);
        Assert.False(success);
        Assert.Same(input, output);
    }

    [Fact]
    public void Translate_ShouldMatchQualifiedPlaceholderInsideReplacement()
    {
        var rule = new GenericPatternTypeNameRule(
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Source"),
                TypeNameFactory.Param("T")),
            new QualifiedName(
                TypeNameFactory.Qualified("Mapped"),
                TypeNameFactory.Param("T")));

        var input = TypeNameFactory.Generic(
            TypeNameFactory.Qualified("Source"),
            TypeNameFactory.Qualified("Domain", "Entity"));

        var success = rule.TryTranslate(input, out var output);
        var renderedInput = Render(input);
        var rendered = Render(output);

        Assert.Equal("Source<Domain::Entity>", renderedInput);
        Assert.True(success);
        Assert.Equal("Mapped::Domain::Entity", rendered);
    }

    [Fact]
    public void Translate_ShouldMatchDeeplyNestedGenericTree()
    {
        var rule = new GenericPatternTypeNameRule(
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Graph"),
                TypeNameFactory.Generic(
                    TypeNameFactory.Qualified("Node"),
                    TypeNameFactory.Param("T")),
                TypeNameFactory.Generic(
                    TypeNameFactory.Qualified("Edge"),
                    TypeNameFactory.Param("T"),
                    TypeNameFactory.Param("U"))),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("GraphData"),
                TypeNameFactory.Param("U"),
                TypeNameFactory.Param("T")));

        var input = TypeNameFactory.Generic(
            TypeNameFactory.Qualified("Graph"),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Node"),
                TypeNameFactory.Qualified("System", "String")),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Edge"),
                TypeNameFactory.Qualified("System", "String"),
                TypeNameFactory.Qualified("System", "Int32")));

        var success = rule.TryTranslate(input, out var output);
        var renderedInput = Render(input);
        var rendered = Render(output);

        Assert.Equal("Graph<Node<Interop::String>, Edge<Interop::String, int32_t>>", renderedInput);
        Assert.True(success);
        Assert.Equal("GraphData<int32_t, Interop::String>", rendered);
    }

    [Fact]
    public void Translate_ShouldReturnFalse_WhenDeeplyNestedGenericTreeConflicts()
    {
        var rule = new GenericPatternTypeNameRule(
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Graph"),
                TypeNameFactory.Generic(
                    TypeNameFactory.Qualified("Node"),
                    TypeNameFactory.Param("T")),
                TypeNameFactory.Generic(
                    TypeNameFactory.Qualified("Edge"),
                    TypeNameFactory.Param("T"),
                    TypeNameFactory.Param("U"))),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("GraphData"),
                TypeNameFactory.Param("U"),
                TypeNameFactory.Param("T")));

        var input = TypeNameFactory.Generic(
            TypeNameFactory.Qualified("Graph"),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Node"),
                TypeNameFactory.Qualified("System", "String")),
            TypeNameFactory.Generic(
                TypeNameFactory.Qualified("Edge"),
                TypeNameFactory.Qualified("System", "Int32"),
                TypeNameFactory.Qualified("System", "Int32")));

        var success = rule.TryTranslate(input, out var output);
        var renderedInput = Render(input);

        Assert.Equal("Graph<Node<Interop::String>, Edge<int32_t, int32_t>>", renderedInput);
        Assert.False(success);
        Assert.Same(input, output);
    }
}
