using System;
using System.Linq;

namespace ReadyM.Api.Generators.TypeTranslation.Model;

internal static class TypeNameFactory
{
    public static ITypeName Empty() => new EmptyName();
    
    public static ITypeName Name(string name) => new TypeName(name);

    public static ITypeName Param(string name) => new TypeParam(name);

    public static ITypeName Number(int value) => new Numeric(value);

    public static ITypeName Qualified(params string[] parts)
    {
        if (parts.Length == 0)
            throw new ArgumentException("At least one part is required.", nameof(parts));
        if (parts.Any(string.IsNullOrEmpty))
            throw new ArgumentException("Parts cannot be null or empty.", nameof(parts));
        if (parts.Any(x => x == null))
            throw new ArgumentException("Parts cannot be null.", nameof(parts));

        ITypeName current = new TypeName(parts[0]);
        
        for (var i = 1; i < parts.Length; i++)
        {
            current = new QualifiedName(current, new TypeName(parts[i]));
        }

        return current;
    }

    public static ITypeName Generic(ITypeName genericDefinition, params ITypeName[] typeArguments) =>
        new GenericInstanceName(genericDefinition, typeArguments);
    
    public static ITypeName Combine(ITypeName prefix, ITypeName innerType)
    {
        if (prefix is EmptyName)
            return innerType;
        return new QualifiedName(prefix, innerType);
    }
}