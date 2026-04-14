using ReadyM.Api.Generators.TypeTranslation.Model;
using ReadyM.Api.Generators.TypeTranslation.Rendering;
using ReadyM.Api.Generators.TypeTranslation.Rules;
using Xunit;

namespace ReadyM.Api.Generators.Tests.TypeTranslation;

public sealed class GenericPatternTypeNameRuleTests
{
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
        var rendered = new CppTypeRenderer().Render(output);

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
        var rendered = new CppTypeRenderer().Render(output);

        Assert.True(success);
        Assert.Equal("Pair<Interop::String, Interop::String>", rendered);
    }
    
    [Fact]
    public void Translate_ShouldNotSubstituteNonMatching()
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
            TypeNameFactory.Qualified("Wrapper2"),
            TypeNameFactory.Qualified("System", "String"));

        var success = rule.TryTranslate(input, out var output2);
        var rendered = new CppTypeRenderer().Render(output2);

        Assert.False(success);
        Assert.Equal("Wrapper2<Interop::String>", rendered);
    }
}
