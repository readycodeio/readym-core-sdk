using System;

namespace ReadyM.Api.Command;

internal interface IConsoleArgumentTypeConversion
{
    bool TryConvert(Type destType, object? source, out object? dest);
}