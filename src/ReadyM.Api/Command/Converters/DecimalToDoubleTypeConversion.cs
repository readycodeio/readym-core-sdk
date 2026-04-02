using System;

namespace ReadyM.Api.Command.Converters;

internal sealed class DecimalToDoubleTypeConversion : IConsoleArgumentTypeConversion
{
    public bool TryConvert(Type destType, object? source, out object? dest)
    {
        if (source is decimal dec && destType == typeof(double))
        {
            dest = (double)dec;
            return true;
        }

        dest = null;
        return false;
    }
}