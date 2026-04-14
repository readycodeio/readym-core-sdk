using System;
using System.Collections.Generic;
using System.Linq;
using ReadyM.Api.Generators.TypeTranslation.Model;

namespace ReadyM.Api.Generators.TypeTranslation.Rules;

public sealed class GenericPatternTypeNameRule(ITypeName pattern, ITypeName replacement) : ITypeNameRule
{
    public bool TryTranslate(ITypeName input, out ITypeName output)
    {
        var bindings = new Dictionary<string, ITypeName>(StringComparer.Ordinal);
        if (TryMatch(pattern, input, bindings))
        {
            output = Substitute(replacement, bindings);
            return true;
        }

        output = input;
        return false;
    }

    private static bool TryMatch(ITypeName patternNode, ITypeName inputNode, Dictionary<string, ITypeName> bindings) =>
        patternNode switch
        {
            TypeParam typeParam => TryBind(typeParam.Name, inputNode, bindings),
            TypeName patternTypeName when inputNode is TypeName inputTypeName => patternTypeName.Name == inputTypeName.Name,
            Numeric patternNumeric when inputNode is Numeric inputNumeric => patternNumeric.Value == inputNumeric.Value,
            QualifiedName patternQualified when inputNode is QualifiedName inputQualified =>
                TryMatch(patternQualified.Prefix, inputQualified.Prefix, bindings)
                && TryMatch(patternQualified.InnerType, inputQualified.InnerType, bindings),
            GenericInstanceName patternGeneric when inputNode is GenericInstanceName inputGeneric =>
                TryMatch(patternGeneric.GenericDefinition, inputGeneric.GenericDefinition, bindings)
                && TryMatchTypeLists(patternGeneric.TypeArguments, inputGeneric.TypeArguments, bindings),
            _ => false,
        };

    private static bool TryMatchTypeLists(
        IReadOnlyList<ITypeName> patternNodes,
        IReadOnlyList<ITypeName> inputNodes,
        Dictionary<string, ITypeName> bindings)
    {
        if (patternNodes.Count != inputNodes.Count)
        {
            return false;
        }

        for (var i = 0; i < patternNodes.Count; i++)
        {
            if (!TryMatch(patternNodes[i], inputNodes[i], bindings))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryBind(string name, ITypeName inputNode, Dictionary<string, ITypeName> bindings)
    {
        if (bindings.TryGetValue(name, out var existing))
        {
            return TypeNameEqualityComparer.Instance.Equals(existing, inputNode);
        }

        bindings.Add(name, inputNode);
        return true;
    }

    private static ITypeName Substitute(ITypeName node, IReadOnlyDictionary<string, ITypeName> bindings) => node switch
    {
        TypeParam typeParam when bindings.TryGetValue(typeParam.Name, out var boundValue) => boundValue,
        QualifiedName qualifiedName => new QualifiedName(
            Substitute(qualifiedName.Prefix, bindings),
            Substitute(qualifiedName.InnerType, bindings)),
        GenericInstanceName genericInstanceName => new GenericInstanceName(
            Substitute(genericInstanceName.GenericDefinition, bindings),
            [.. genericInstanceName.TypeArguments.Select(x => Substitute(x, bindings))]),
        _ => node,
    };
}