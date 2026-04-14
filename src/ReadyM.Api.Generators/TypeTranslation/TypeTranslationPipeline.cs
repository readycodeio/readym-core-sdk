using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.TypeTranslation.Model;
using ReadyM.Api.Generators.TypeTranslation.Parsing;
using ReadyM.Api.Generators.TypeTranslation.Rendering;
using ReadyM.Api.Generators.TypeTranslation.Rules;

namespace ReadyM.Api.Generators.TypeTranslation;

public sealed class TypeTranslationPipeline(
    ITypeNameParser parser,
    TypeNameTranslator translator,
    ITypeRenderer renderer)
{
    private ITypeName Parse(ITypeSymbol typeSymbol) => parser.Parse(typeSymbol);

    private ITypeName Translate(ITypeName typeName) => translator.Translate(typeName);

    private string Render(ITypeName typeName) => renderer.Render(typeName);

    public string Translate(ITypeSymbol typeSymbol)
        => Render(Translate(Parse(typeSymbol)));
}