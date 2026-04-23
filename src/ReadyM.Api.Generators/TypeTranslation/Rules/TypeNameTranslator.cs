using System.Collections.Generic;
using System.Linq;
using ReadyM.Api.Generators.TypeTranslation.Model;

namespace ReadyM.Api.Generators.TypeTranslation.Rules;

public sealed class TypeNameTranslator(IReadOnlyList<ITypeNameRule> rules)
{
    public ITypeName Translate(ITypeName input)
    {
        foreach (var rule in rules)
        {
            if (rule.TryTranslate(input, out var output))
            {
                return output;
            }
        }

        return TranslateChildren(input);
    }

    private ITypeName TranslateChildren(ITypeName input) => input switch
    {
        QualifiedName qualifiedName => new QualifiedName(
            Translate(qualifiedName.Prefix),
            Translate(qualifiedName.InnerType)),
        GenericInstanceName genericInstanceName => new GenericInstanceName(
            Translate(genericInstanceName.GenericDefinition),
            [.. genericInstanceName.TypeArguments.Select(Translate)]),
        _ => input,
    };
}