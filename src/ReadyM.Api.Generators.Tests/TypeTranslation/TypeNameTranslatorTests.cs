using ReadyM.Api.Generators.TypeTranslation.Model;
using ReadyM.Api.Generators.TypeTranslation.Rendering;
using ReadyM.Api.Generators.TypeTranslation.Rules;
using Xunit;

namespace ReadyM.Api.Generators.Tests.TypeTranslation;

public sealed class TypeNameTranslatorTests
{
    [Fact]
    public void Translate_ShouldApplyRulesInOrder()
    {
        var translator = new TypeNameTranslator(
        [
            new ExactTypeReplacementRule(
                TypeNameFactory.Qualified("Some", "Name", "Space", "Type"),
                TypeNameFactory.Qualified("First")),
            new NamespaceReplacementRule(
                TypeNameFactory.Qualified("Some", "Name", "Space"),
                TypeNameFactory.Qualified("Second")),
        ]);

        var input = TypeNameFactory.Qualified("Some", "Name", "Space", "Type");
        var output = translator.Translate(input);
        var rendered = new CppTypeRenderer().Render(output);

        Assert.Equal("First", rendered);
    }

    [Fact]
    public void Translate_ShouldRecurseIntoGenericArguments_WhenNoTopLevelRuleMatches()
    {
        var translator = new TypeNameTranslator(
        [
            new NamespaceReplacementRule(
                TypeNameFactory.Qualified("Some", "Name", "Space"),
                TypeNameFactory.Qualified("Other")),
        ]);

        var input = TypeNameFactory.Generic(
            TypeNameFactory.Name("Wrapper"),
            TypeNameFactory.Qualified("Some", "Name", "Space", "Leaf"));

        var output = translator.Translate(input);
        var rendered = new CppTypeRenderer().Render(output);

        Assert.Equal("Wrapper<Other::Leaf>", rendered);
    }

    [Fact]
    public void Translate_ShouldReturnInput_WhenNoRuleMatchesAndNoChildrenChange()
    {
        var translator = new TypeNameTranslator([]);
        var input = TypeNameFactory.Qualified("A", "B", "C");

        var output = translator.Translate(input);
        var rendered = new CppTypeRenderer().Render(output);

        Assert.Equal("A::B::C", rendered);
    }

    [Fact]
    public void Translate_ShouldRecurseIntoQualifiedNamePrefix()
    {
        var translator = new TypeNameTranslator(
        [
            new NamespaceReplacementRule(
                TypeNameFactory.Qualified("Old", "Ns"),
                TypeNameFactory.Qualified("New", "Ns")),
        ]);

        var input = TypeNameFactory.Qualified("Old", "Ns", "Leaf");

        var output = translator.Translate(input);
        var rendered = new CppTypeRenderer().Render(output);

        Assert.Equal("New::Ns::Leaf", rendered);
    }

    [Fact]
    public void Translate_ShouldApplyTopLevelRuleBeforeTranslatingChildren()
    {
        var translator = new TypeNameTranslator(
        [
            new ExactTypeReplacementRule(
                TypeNameFactory.Generic(
                    TypeNameFactory.Name("Wrapper"),
                    TypeNameFactory.Qualified("Old", "Ns", "Leaf")),
                TypeNameFactory.Name("Collapsed")),
            new NamespaceReplacementRule(
                TypeNameFactory.Qualified("Old", "Ns"),
                TypeNameFactory.Qualified("New", "Ns")),
        ]);

        var input = TypeNameFactory.Generic(
            TypeNameFactory.Name("Wrapper"),
            TypeNameFactory.Qualified("Old", "Ns", "Leaf"));

        var output = translator.Translate(input);
        var rendered = new CppTypeRenderer().Render(output);

        Assert.Equal("Collapsed", rendered);
    }
}
