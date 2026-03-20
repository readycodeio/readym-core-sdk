using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadyM.Api.Command;

internal sealed class ConsoleCommandRegistry : IConsoleCommandRegistry
{
    private readonly Dictionary<string, ConsoleCommand> _commands = new();
    private readonly Dictionary<string, IEnumerable<string>> _commandsParams = new();

    public ConsoleCommandRegistry(IEnumerable<IConsoleCommandRegistration> registrations)
    {
        AddCommands(registrations);
    }

    public void AddCommands(IEnumerable<IConsoleCommandRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            AddCommand(registration);
        }
    }

    public void AddCommand(IConsoleCommandRegistration registration)
    {
        registration.RegisterCommands(this);
    }

    public void AddCommand(string commandName, ConsoleCommand command, IEnumerable<string>? availableFirstParams = null)
    {
        if (!_commands.TryAdd(commandName, command))
        {
            throw new InvalidOperationException($"Command {commandName} is already registered");
        }

        if (availableFirstParams != null)
        {
            _commandsParams[commandName] = availableFirstParams;
        }
    }

    public bool HasCommand(string commandName)
        => _commands.ContainsKey(commandName);

    public ConsoleCommand GetCommand(string commandName)
        => _commands[commandName];

    public List<string> GetCommandAvailableFirstParams(string commandName)
        => _commandsParams.TryGetValue(commandName, out var paramsList) ? paramsList.ToList() : [];

    public IEnumerable<string> GetCommandNames(bool includeDebug)
        => _commands.Where(x => includeDebug || !x.Value.IsDebugOnly).Select(x => x.Key);
}