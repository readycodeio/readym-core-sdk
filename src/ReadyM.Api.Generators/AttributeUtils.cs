using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

public static class AttributeUtils
{
    public static T GetAttribute<T>(INamedTypeSymbol? symbol, string attrName, string keyName, T defaultValue)
    {
        var deriveData = symbol?.GetAttributes()
            .FirstOrDefault(ad => ad.AttributeClass?.Name == attrName || ad.AttributeClass?.ToDisplayString().EndsWith($".{attrName}") == true);

        object? objValue = null;
        if (deriveData is not null)
        {
            var modeAttr = deriveData.NamedArguments.FirstOrDefault(x => x.Key == keyName);
            if (modeAttr.Value.Value != null)
            {
                objValue = modeAttr.Value.Value;
            }
            else if (deriveData.ConstructorArguments.Length > 0)
            {
                var posConst = deriveData.ConstructorArguments[0];
                objValue  = posConst.Value;
            }
        }

        if (objValue is T value)
            return value;
        else
            return defaultValue;
    }
}