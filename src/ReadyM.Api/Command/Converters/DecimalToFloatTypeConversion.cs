using System;

namespace ReadyM.Api.Command.Converters;

public sealed class DecimalToFloatTypeConversion : IConsoleArgumentTypeConversion
{
    public bool TryConvert(Type destType, object? source, out object? dest)
    {
        if (source is decimal dec && destType == typeof(float))
        {
            dest = (float)dec;
            return true;
        }

        dest = null;
        return false;
    }
}