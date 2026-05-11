using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ReadyM.Api.Generators.TypeTranslation.Parsing;
using ReadyM.Api.Generators.TypeTranslation.Rendering;
using Xunit;

namespace ReadyM.Api.Generators.Tests.TypeTranslation;

public sealed class RoslynTypeNameParserTests
{
    [Fact]
    public void Parse_ShouldParseQualifiedGenericType()
    {
        var compilation = CSharpCompilation.Create(
            "Tests",
            [
                CSharpSyntaxTree.ParseText(
                    """
                    namespace Name.Space;

                    public sealed class SomeType<X, Y>
                    {
                    }

                    public sealed class Holder
                    {
                        public SomeType<int, string> Value => throw null!;
                    }
                    """)
            ],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            ]);

        var holder = compilation.GetTypeByMetadataName("Name.Space.Holder")!;
        var property = holder.GetMembers("Value").OfType<IPropertySymbol>().Single();

        var parser = new RoslynTypeNameParser();
        var parsed = parser.Parse(property.Type);
        var rendered = new CppTypeRenderer().Render(parsed);

        Assert.Equal("Name::Space::SomeType<int32_t, Interop::String>", rendered);
    }

    [Fact]
    public void Parse_ShouldUseCSharpAliasesForSpecialTypes()
    {
        var compilation = CSharpCompilation.Create(
            "Tests",
            [
                CSharpSyntaxTree.ParseText(
                    """
                    namespace Name.Space;

                    public sealed class Holder
                    {
                        public int Value => 0;
                    }
                    """)
            ],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            ]);

        var holder = compilation.GetTypeByMetadataName("Name.Space.Holder")!;
        var property = holder.GetMembers("Value").OfType<IPropertySymbol>().Single();

        var parser = new RoslynTypeNameParser();
        var parsed = parser.Parse(property.Type);
        var rendered = new CppTypeRenderer().Render(parsed);

        Assert.Equal("int32_t", rendered);
    }

    [Fact]
    public void Parse_ShouldParseTypeParameter()
    {
        var compilation = CSharpCompilation.Create(
            "Tests",
            [
                CSharpSyntaxTree.ParseText(
                    """
                    namespace Name.Space;

                    public sealed class Holder<T>
                    {
                        public T Value => throw null!;
                    }
                    """)
            ],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            ]);

        var holder = compilation.GetTypeByMetadataName("Name.Space.Holder`1")!;
        var property = holder.GetMembers("Value").OfType<IPropertySymbol>().Single();

        var parser = new RoslynTypeNameParser();
        var parsed = parser.Parse(property.Type);
        var rendered = new CppTypeRenderer().Render(parsed);

        Assert.Equal("T", rendered);
    }

    [Fact]
    public void Parse_ShouldParseNestedGenericArguments()
    {
        var compilation = CSharpCompilation.Create(
            "Tests",
            [
                CSharpSyntaxTree.ParseText(
                    """
                    using System.Collections.Generic;

                    namespace Name.Space;

                    public sealed class Holder
                    {
                        public Dictionary<int, List<string>> Value => throw null!;
                    }
                    """)
            ],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.Dictionary<,>).Assembly.Location)
            ]);

        var holder = compilation.GetTypeByMetadataName("Name.Space.Holder")!;
        var property = holder.GetMembers("Value").OfType<IPropertySymbol>().Single();

        var parser = new RoslynTypeNameParser();
        var parsed = parser.Parse(property.Type);
        var rendered = new CppTypeRenderer().Render(parsed);

        Assert.Equal("System::Collections::Generic::Dictionary<int32_t, System::Collections::Generic::List<Interop::String>>", rendered);
    }

    [Fact]
    public void Parse_ShouldIncludeContainingTypes()
    {
        var compilation = CSharpCompilation.Create(
            "Tests",
            [
                CSharpSyntaxTree.ParseText(
                    """
                    namespace Name.Space;

                    public sealed class Outer
                    {
                        public sealed class Inner
                        {
                        }
                    }

                    public sealed class Holder
                    {
                        public Outer.Inner Value => throw null!;
                    }
                    """)
            ],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            ]);

        var holder = compilation.GetTypeByMetadataName("Name.Space.Holder")!;
        var property = holder.GetMembers("Value").OfType<IPropertySymbol>().Single();

        var parser = new RoslynTypeNameParser();
        var parsed = parser.Parse(property.Type);
        var rendered = new CppTypeRenderer().Render(parsed);

        Assert.Equal("Name::Space::Outer::Inner", rendered);
    }
}
