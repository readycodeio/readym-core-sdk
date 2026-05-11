using ReadyM.Api.Generators.TypeTranslation.Model;

namespace ReadyM.Api.Generators.TypeTranslation.Rules;

public interface ITypeNameRule
{
    bool TryTranslate(ITypeName input, out ITypeName output);
}