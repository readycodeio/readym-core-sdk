using System.Collections.Generic;
using System.Linq;

namespace ReadyM.Api.Command;

internal readonly struct ParsedCommandCall(string commandName, IEnumerable<object?> args)
{
    public readonly string CommandName = commandName;
    public readonly object?[] Args = args.ToArray();
}