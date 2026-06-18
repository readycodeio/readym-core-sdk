using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api.Command;

internal sealed class ConsoleCommandRegistry
{
    private readonly ConsoleCommandParser _parser;
    private readonly Dictionary<string, ConsoleCommand> _commands = new();
    private readonly Dictionary<string, Func<IEnumerable<string>>> _commandsParams = new();

    public ConsoleCommandRegistry(ConsoleCommandParser parser, IEnumerable<IConsoleCommandRegistration> registrations)
    {
        _parser = parser;
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

    public void AddCommand(string commandName, ConsoleCommand command, Func<IEnumerable<string>>? availableFirstParams = null)
    {
        if (!_parser.IsCommandNameValid(commandName, out var errorMessage))
        {
            throw new Exception($"Invalid command name: '{commandName}'. {errorMessage}");
        }

        if (!_commands.TryAdd(commandName, command))
        {
            throw new InvalidOperationException($"Command '{commandName}' is already registered");
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
        => _commandsParams.TryGetValue(commandName, out var paramsList) ? paramsList().ToList() : [];

    public IEnumerable<string> GetCommandNames(bool includeDebug)
        => _commands.Where(x => includeDebug || !x.Value.IsDebugOnly).Select(x => x.Key);
}