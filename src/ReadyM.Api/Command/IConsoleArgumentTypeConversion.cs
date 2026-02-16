using System;

namespace ReadyM.Api.Command;

public interface IConsoleArgumentTypeConversion
{
    bool TryConvert(Type destType, object? source, out object? dest);
}