using System.Collections.Generic;

namespace ReadyM.Api.Command;

public interface IConsoleCommandRegistry
{
    void AddCommand(string commandName, ConsoleCommand command, IEnumerable<string>? availableFirstParams = null);
    bool HasCommand(string commandName);
}