namespace ReadyM.Api.Command;

internal interface IConsoleCommandRegistration
{
    void RegisterCommands(ConsoleCommandRegistry registry);
}