using System;
using Superpower.Model;

namespace ReadyM.Api.Command;

internal abstract record CommandError
{
    private CommandError() { }

    internal sealed record InvalidCommandFormat(string Input, Exception? Exception) : CommandError;
    internal sealed record InvalidArgumentFormat(string Input, int ArgIndex, Position Position) : CommandError;
    internal sealed record UnrecognizedCommand(string CommandName) : CommandError;
    internal sealed record TooFewArguments(int MinCount, int ActualCount) : CommandError;
    internal sealed record TooManyArguments(int MaxCount, int ActualCount) : CommandError;
    internal sealed record InvalidArgumentType(int ArgIndex, Type ExpectedType, Type ActualType) : CommandError;
    internal sealed record ExecutionError(Exception Exception) : CommandError;
}
