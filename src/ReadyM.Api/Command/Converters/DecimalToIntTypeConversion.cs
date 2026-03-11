using System;

namespace ReadyM.Api.Command.Converters;

public sealed class DecimalToIntTypeConversion : IConsoleArgumentTypeConversion
{
    public bool TryConvert(Type destType, object? source, out object? dest)
    {
        if (source is decimal dec && destType == typeof(int))
        {
            // only parse if it's a whole number and within int range
            if (dec % 1 == 0 && dec >= int.MinValue && dec <= int.MaxValue)
            {
                dest = (int)dec;
                return true;
            }
        }

        dest = null;
        return false;
    }
}