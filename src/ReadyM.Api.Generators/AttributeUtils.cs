using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

public static class AttributeUtils
{
    public static bool HasAttribute(ISymbol symbol, string attrName)
    {
        return symbol.GetAttributes()
            .Any(ad =>
                ad.AttributeClass is not null &&
                (ad.AttributeClass.Name == attrName ||
                 ad.AttributeClass.Name == attrName + "Attribute" ||
                 ad.AttributeClass.ToDisplayString().EndsWith("." + attrName, StringComparison.Ordinal) ||
                 ad.AttributeClass.ToDisplayString().EndsWith("." + attrName + "Attribute", StringComparison.Ordinal)));
    }
    
    public static T GetAttribute<T>(
        ISymbol? symbol,
        string attrName,
        string keyName,
        T defaultValue)
    {
        if (symbol is null)
            return defaultValue;

        var attr = symbol.GetAttributes()
            .FirstOrDefault(ad =>
                ad.AttributeClass is not null &&
                (ad.AttributeClass.Name == attrName ||
                 ad.AttributeClass.Name == attrName + "Attribute" ||
                 ad.AttributeClass.ToDisplayString().EndsWith("." + attrName, StringComparison.Ordinal) ||
                 ad.AttributeClass.ToDisplayString().EndsWith("." + attrName + "Attribute", StringComparison.Ordinal)));

        if (attr is null)
            return defaultValue;

        return GetAttributeValue(attr, keyName, defaultValue);
    }

    public static T GetAttributeValue<T>(
        AttributeData attr,
        string keyName,
        T defaultValue)
    {
        foreach (var named in attr.NamedArguments)
        {
            if (string.Equals(named.Key, keyName))
                return ConvertValue<T>(named.Value.Value, defaultValue);
        }

        var ctor = attr.AttributeConstructor;
        if (ctor is not null)
        {
            for (int i = 0; i < ctor.Parameters.Length; i++)
            {
                var param = ctor.Parameters[i];

                if (!string.Equals(param.Name, keyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (i < attr.ConstructorArguments.Length)
                    return ConvertValue<T>(attr.ConstructorArguments[i], defaultValue);

                if (param.HasExplicitDefaultValue)
                    return ConvertValue<T>(param.ExplicitDefaultValue, defaultValue);

                return defaultValue;
            }
        }

        return defaultValue;
    }
    
    public static IReadOnlyList<T>? GetArrayAttribute<T>(
        ISymbol? symbol,
        string attrName,
        string keyName,
        params T[] defaultValues)
        => GetAttribute<IReadOnlyList<T>>(symbol, attrName, keyName, defaultValues);

    private static T ConvertValue<T>(TypedConstant typedConst, T defaultValue)
    {
        switch (typedConst.Kind)
        {
            case TypedConstantKind.Enum:
            case TypedConstantKind.Primitive:
            case TypedConstantKind.Type:
                return ConvertValue(typedConst.Value, defaultValue);

            case TypedConstantKind.Array:
                return ConvertArrayValue(typedConst, defaultValue);

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(typedConst),
                    "Unexpected TypedConstantKind: " + typedConst.Kind);
        }
    }
    
    private static T ConvertArrayValue<T>(TypedConstant typedConst, T defaultValue)
    {
        var targetType = typeof(T);

        if (!TryGetCollectionElementType(targetType, out var elementType))
            return defaultValue;

        var values = typedConst.Values;

        var array = Array.CreateInstance(elementType, values.Length);

        for (var i = 0; i < values.Length; i++)
        {
            var itemTypedConst = values[i];

            object? rawValue = itemTypedConst.Kind switch
            {
                TypedConstantKind.Array => ConvertArrayValue<object?>(itemTypedConst, null),
                _ => itemTypedConst.Value
            };

            var convertedItem = rawValue is null
                ? null
                : Convert.ChangeType(rawValue, Nullable.GetUnderlyingType(elementType) ?? elementType);

            array.SetValue(convertedItem, i);
        }

        if (targetType.IsArray)
            return (T)(object)array;

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = Activator.CreateInstance(listType)!;

        var addMethod = listType.GetMethod(nameof(List<object>.Add))!;

        foreach (var item in array)
            addMethod.Invoke(list, new[] { item });

        if (targetType.IsAssignableFrom(listType))
            return (T)list;

        if (targetType.IsInterface && targetType.IsAssignableFrom(list.GetType()))
            return (T)list;

        return defaultValue;
    }
    
    private static bool TryGetCollectionElementType(Type targetType, out Type elementType)
    {
        if (targetType.IsArray)
        {
            elementType = targetType.GetElementType()!;
            return true;
        }

        if (targetType.IsGenericType)
        {
            var genericTypeDefinition = targetType.GetGenericTypeDefinition();

            if (genericTypeDefinition == typeof(List<>) ||
                genericTypeDefinition == typeof(IList<>) ||
                genericTypeDefinition == typeof(IReadOnlyList<>) ||
                genericTypeDefinition == typeof(ICollection<>) ||
                genericTypeDefinition == typeof(IReadOnlyCollection<>) ||
                genericTypeDefinition == typeof(IEnumerable<>))
            {
                elementType = targetType.GetGenericArguments()[0];
                return true;
            }
        }

        var enumerableInterface = targetType
            .GetInterfaces()
            .FirstOrDefault(x =>
                x.IsGenericType &&
                x.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableInterface is not null)
        {
            elementType = enumerableInterface.GetGenericArguments()[0];
            return true;
        }

        elementType = null!;
        return false;
    }
    
    private static T ConvertValue<T>(object? value, T defaultValue)
    {
        if (value is null)
            return defaultValue;

        if (value is T typed)
            return typed;

        var targetType = typeof(T);

        if (targetType.IsEnum)
        {
            try
            {
                return (T)Enum.ToObject(targetType, value);
            }
            catch
            {
                return defaultValue;
            }
        }

        try
        {
            return (T)Convert.ChangeType(value, targetType);
        }
        catch
        {
            return defaultValue;
        }
    }
}