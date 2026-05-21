using ReadyM.Api.Generators.TypeTranslation.Model;

namespace ReadyM.Api.Generators.TypeTranslation.Rules;

internal sealed class ExactTypeReplacementRule(ITypeName source, ITypeName target) : ITypeNameRule
{
    public bool TryTranslate(ITypeName input, out ITypeName output)
    {
        if (TypeNameEqualityComparer.Instance.Equals(input, source))
        {
            output = target;
            return true;
        }

        output = input;
        return false;
    }
}