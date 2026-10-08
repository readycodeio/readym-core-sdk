using System;
using System.Collections.Generic;
using System.Linq;
using ReadyM.Api.Command;

namespace ReadyM.SDK.Commands;

public sealed class CommandRegistry
{
    private readonly Dictionary<string, CommandDescriptor> _commands =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConsoleCommandRegistry _console;

    public CommandRegistry()
    {
        _console = new ConsoleCommandRegistry(Parser, []);
    }

    internal ConsoleCommandParser Parser { get; } = new([new StandardArgumentParserRegistration()]);

    internal ConsoleCommandRegistry Console => _console;

    public IEnumerable<CommandDescriptor> All =>
        _commands.Values.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase);

    public CommandDescriptor Add(string name, Delegate handler, string usage = "", string? permission = null)
    {
        var descriptor = new CommandDescriptor(name, handler, usage, permission);

        if (!_commands.TryAdd(descriptor.Name, descriptor))
            throw new ArgumentException($"/{descriptor.Name} is already registered.", nameof(name));

        _console.AddCommand(descriptor.Name, descriptor.Console);

        return descriptor;
    }

    public bool TryGet(string name, out CommandDescriptor command) => _commands.TryGetValue(name, out command!);
}
