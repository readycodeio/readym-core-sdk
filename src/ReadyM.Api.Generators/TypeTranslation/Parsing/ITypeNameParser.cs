using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.TypeTranslation.Model;

namespace ReadyM.Api.Generators.TypeTranslation.Parsing;

internal interface ITypeNameParser
{
    ITypeName Parse(ITypeSymbol typeSymbol);
}