
using System;
using Superpower.Model;

namespace ReadyM.Api.Command;

public abstract record CommandError
{
    private CommandError() { }

    public sealed record InvalidCommandFormat(string Input, Exception? Exception) : CommandError;
    public sealed record InvalidArgumentFormat(string Input, int ArgIndex, Position Position) : CommandError;
    public sealed record UnrecognizedCommand(string CommandName) : CommandError;
    public sealed record TooFewArguments(int MinCount, int ActualCount) : CommandError;
    public sealed record TooManyArguments(int MaxCount, int ActualCount) : CommandError;
    public sealed record InvalidArgumentType(int ArgIndex, Type ExpectedType, Type ActualType) : CommandError;
    public sealed record ExecutionError(Exception Exception) : CommandError;
}
