using ReadyM.Api.Generators.TypeTranslation.Model;

namespace ReadyM.Api.Generators.TypeTranslation.Rules;

public sealed class NamespaceReplacementRule(ITypeName source, ITypeName target) : ITypeNameRule
{
    public bool TryTranslate(ITypeName input, out ITypeName output)
    {
        if (TypeNameEqualityComparer.Instance.Equals(input, source))
        {
            output = target;
            return true;
        }

        if (input is QualifiedName qualifiedName && TypeNameEqualityComparer.Instance.Equals(qualifiedName.Prefix, source))
        {
            output = TypeNameFactory.Combine(target, qualifiedName.InnerType);
            return true;
        }

        output = input;
        return false;
    }
}