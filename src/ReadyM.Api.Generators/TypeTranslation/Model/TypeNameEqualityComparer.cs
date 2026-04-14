using System;
using System.Collections.Generic;

namespace ReadyM.Api.Generators.TypeTranslation.Model;

public sealed class TypeNameEqualityComparer : IEqualityComparer<ITypeName>
{
    public static TypeNameEqualityComparer Instance { get; } = new();

    public bool Equals(ITypeName? x, ITypeName? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        return (x, y) switch
        {
            (TypeName left, TypeName right)
                => left.Name == right.Name,
            (TypeParam left, TypeParam right)
                => left.Name == right.Name,
            (Numeric left, Numeric right)
                => left.Value == right.Value,
            (QualifiedName left, QualifiedName right) =>
                Equals(left.Prefix, right.Prefix)
                && Equals(left.InnerType, right.InnerType),
            (GenericInstanceName left, GenericInstanceName right) =>
                Equals(left.GenericDefinition, right.GenericDefinition) &&
                EqualsLists(left.TypeArguments, right.TypeArguments),
            _ => false,
        };
    }

    public int GetHashCode(ITypeName obj)
    {
        switch (obj)
        {
            case TypeName typeName:
            {
                return StringComparer.Ordinal.GetHashCode(typeName.Name);
            }
            case TypeParam typeParam:
            {
                var h = 17;
                h = h * 31 + typeof(TypeParam).GetHashCode();
                h = h * 31 + StringComparer.Ordinal.GetHashCode(typeParam.Name);
                return h;
            }
            case Numeric numeric:
            {
                var h = 17;
                h = h * 31 + typeof(Numeric).GetHashCode();
                h = h * 31 + numeric.Value.GetHashCode();
                return h;
            }
            case QualifiedName qualifiedName:
            {
                var h = 17;
                h = h * 31 + typeof(QualifiedName).GetHashCode();
                h = h * 31 + GetHashCode(qualifiedName.Prefix);
                h = h * 31 + GetHashCode(qualifiedName.InnerType);
                return h;
            }
            case GenericInstanceName genericInstanceName:
            {
                var h = 17;
                h = h * 31 + typeof(GenericInstanceName).GetHashCode();
                h = h * 31 + GetHashCode(genericInstanceName.GenericDefinition);
                h = h * 31 + GetListHashCode(genericInstanceName.TypeArguments);
                return h;
            }
            default:
                throw new NotSupportedException($"Unsupported type name kind: {obj.GetType().FullName}");
        }
    }

    private bool EqualsLists(IReadOnlyList<ITypeName> left, IReadOnlyList<ITypeName> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (!Equals(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }

    private int GetListHashCode(IReadOnlyList<ITypeName> items)
    {
        var h = 17;
        foreach (var item in items)
        {
            h = h * 31 + GetHashCode(item);
        }

        return h;
    }
}