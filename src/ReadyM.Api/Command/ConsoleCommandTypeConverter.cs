using System;
using System.Collections.Generic;

namespace ReadyM.Api.Command;

public class ConsoleCommandTypeConverter(IReadOnlyList<IConsoleCommandTypeConversion> conversions)
{
    public bool CanConvert(Type destType, object? source)
    {
        if (destType.IsInstanceOfType(source))
            return true;
        
        foreach (var conversion in conversions)
        {
            if (conversion.TryConvert(destType, source, out _))
                return true;
        }

        return false;
    }

    public bool TryConvert(Type destType, object? source, out object? dest)
    {
        if (destType.IsInstanceOfType(source))
        {
            dest = source;
            return true;
        }
        
        foreach (var conversion in conversions)
        {
            if (conversion.TryConvert(destType, source, out dest))
                return true;
        }

        dest = null;
        return false;
    }
}