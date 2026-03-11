using System;

namespace ReadyM.Api.Command.Converters;

public sealed class IdentToStringTypeConversion : IConsoleArgumentTypeConversion
{
    public bool TryConvert(Type destType, object? source, out object? dest)
    {
        if (source is Ident ident && destType == typeof(string))
        {
            dest = ident.Name;
            return true;
        }

        dest = null;
        return false;
    }
}