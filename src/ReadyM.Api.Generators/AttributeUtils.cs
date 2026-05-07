using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

public static class AttributeUtils
{
    public static bool HasAttribute(ISymbol symbol, string deriveinetworkedcomponentattribute)
    {
        return symbol.GetAttributes()
            .Any(ad =>
                ad.AttributeClass is not null &&
                (ad.AttributeClass.Name == deriveinetworkedcomponentattribute ||
                 ad.AttributeClass.Name == deriveinetworkedcomponentattribute + "Attribute" ||
                 ad.AttributeClass.ToDisplayString().EndsWith("." + deriveinetworkedcomponentattribute, StringComparison.Ordinal) ||
                 ad.AttributeClass.ToDisplayString().EndsWith("." + deriveinetworkedcomponentattribute + "Attribute", StringComparison.Ordinal)));
    }
    
    public static T GetAttribute<T>(
        INamedTypeSymbol? symbol,
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
                    return ConvertValue<T>(attr.ConstructorArguments[i].Value, defaultValue);

                if (param.HasExplicitDefaultValue)
                    return ConvertValue<T>(param.ExplicitDefaultValue, defaultValue);

                return defaultValue;
            }
        }

        return defaultValue;
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