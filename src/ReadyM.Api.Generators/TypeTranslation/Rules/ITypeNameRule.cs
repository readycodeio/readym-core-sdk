using ReadyM.Api.Generators.TypeTranslation.Model;

namespace ReadyM.Api.Generators.TypeTranslation.Rules;

internal interface ITypeNameRule
{
    bool TryTranslate(ITypeName input, out ITypeName output);
}