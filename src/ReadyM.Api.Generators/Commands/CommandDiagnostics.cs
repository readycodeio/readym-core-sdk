using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Commands;

/// What a [Command] method is refused for, said at the declaration rather than at a call that never compiles.
internal static class CommandDiagnostics
{
    private const string Category = "ReadyM.Commands";

    public static readonly DiagnosticDescriptor NeedsAName = new(
        "RMCMD001", "A command needs a name",
        "[Command] on '{0}' has no name, and a command with no name can never be typed",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NeedsAContext = new(
        "RMCMD002", "A command takes a context first",
        "'{0}' must take CommandContext as its first parameter",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CannotBeStatic = new(
        "RMCMD003", "A command is an instance method",
        "'{0}' is static, and a command is registered from the instance that declares it",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
}
