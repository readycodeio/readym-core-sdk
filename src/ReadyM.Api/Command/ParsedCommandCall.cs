using System.Collections.Generic;

namespace ReadyM.Api.Command;

public readonly struct ParsedCommandCall(string commandName, IReadOnlyList<object> args)
{
    public readonly string CommandName = commandName;
    public readonly IReadOnlyList<object> Args = args;
}