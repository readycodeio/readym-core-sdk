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
    public ITypeName Parse(ITypeSymbol typeSymbol) => parser.Parse(typeSymbol);

    public ITypeName Translate(ITypeName typeName) => translator.Translate(typeName);

    public string Render(ITypeName typeName) => renderer.Render(typeName);

    public string Translate(ITypeSymbol typeSymbol)
        => Render(Translate(Parse(typeSymbol)));
}