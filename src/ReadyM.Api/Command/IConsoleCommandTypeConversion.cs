using System;

namespace ReadyM.Api.Command;

public interface IConsoleCommandTypeConversion
{
    bool TryConvert(Type destType, object? source, out object? dest);
}