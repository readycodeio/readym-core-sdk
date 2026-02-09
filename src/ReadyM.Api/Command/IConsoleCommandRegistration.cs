namespace ReadyM.Api.Command;

public interface IConsoleCommandRegistration
{
    void RegisterCommands(ConsoleCommandRegistry registry);
}