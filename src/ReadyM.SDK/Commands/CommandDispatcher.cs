using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Command;
using ReadyM.Api.Command.Converters;

namespace ReadyM.SDK.Commands;

public sealed class CommandDispatcher
{
    private readonly CommandRegistry _registry;
    private readonly ICommandPermissions _permissions;
    private readonly ILogger _logger;
    private readonly ConsoleCommandMatcher _matcher;

    public CommandDispatcher(CommandRegistry registry, ICommandPermissions permissions, ILogger logger)
    {
        _registry = registry;
        _permissions = permissions;
        _logger = logger;

        _matcher = new ConsoleCommandMatcher(
            registry.Parser,
            registry.Console,
            new ConsoleArgumentTypeConverter([
                new DecimalToIntTypeConversion(),
                new DecimalToFloatTypeConversion(),
                new DecimalToDoubleTypeConversion(),
                new IdentToStringTypeConversion(),
            ]));
    }

    public CommandOutcome Dispatch(string line, CommandContext context)
    {
        var name = FirstWord(line, out var rest);

        if (name.Length == 0 || !_registry.TryGet(name, out var command))
        {
            context.Output.Refuse($"There is no /{name}. Try /help.");
            return CommandOutcome.Unknown;
        }

        if (command.Permission is { } permission && !_permissions.IsAllowed(context.Caller, permission))
        {
            context.Output.Refuse($"/{name} is not yours to run.");
            return CommandOutcome.Denied;
        }

        if (!_matcher.TryMatch(Rebuild(name, rest, command.Binding), out var console, out var call, out var error))
        {
            context.Output.Refuse(Describe(error, command.Usage));
            return CommandOutcome.BadArguments;
        }

        var arguments = new object?[call.Value.Args.Length + 1];
        arguments[0] = context;
        Array.Copy(call.Value.Args, 0, arguments, 1, call.Value.Args.Length);

        try
        {
            console.Value.Handler.DynamicInvoke(arguments);
            return CommandOutcome.Ran;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "/{Command} threw for {Caller}.", name, context.Caller);
            context.Output.Refuse($"/{name} failed.");
            return CommandOutcome.Threw;
        }
    }

    /// Puts the line back for the matcher, quoting the rest before the tokenizer loses its spacing.
    private static string Rebuild(string name, string rest, CommandBinding binding)
    {
        if (binding.RemainderIndex is not { } remainderIndex) return rest.Length == 0 ? name : name + " " + rest;

        var parts = new List<string>();
        var remainder = rest;

        for (var i = 0; i < remainderIndex; i++)
        {
            var word = FirstWord(remainder, out remainder);
            if (word.Length == 0) break;

            parts.Add(word);
        }

        var line = new StringBuilder(name);
        foreach (var part in parts) line.Append(' ').Append(part);

        // Left out rather than quoted as "", so arity refuses it and the caller gets the usage line.
        if (remainder.Length > 0) line.Append(' ').Append(Quote(remainder));

        return line.ToString();
    }

    private static string Quote(string text)
    {
        var quoted = new StringBuilder(text.Length + 2).Append('"');

        foreach (var c in text)
        {
            switch (c)
            {
                case '\\': quoted.Append("\\\\"); break;
                case '\"': quoted.Append("\\\""); break;
                case '\n': quoted.Append("\\n"); break;
                case '\r': quoted.Append("\\r"); break;
                case '\t': quoted.Append("\\t"); break;
                default: quoted.Append(c); break;
            }
        }

        return quoted.Append('"').ToString();
    }

    private static string FirstWord(string text, out string rest)
    {
        var trimmed = text.TrimStart();
        var space = trimmed.IndexOf(' ');

        if (space < 0)
        {
            rest = string.Empty;
            return trimmed;
        }

        rest = trimmed.Substring(space + 1).TrimStart();
        return trimmed.Substring(0, space);
    }

    private static string Describe(CommandError? error, string usage)
        => error switch
        {
            CommandError.TooFewArguments or CommandError.TooManyArguments => $"Usage: {usage}",
            CommandError.InvalidArgumentType t =>
                $"Argument {t.ArgIndex + 1} should be {Friendly(t.ExpectedType)}. Usage: {usage}",
            CommandError.InvalidArgumentFormat => $"That did not parse. Usage: {usage}",
            CommandError.InvalidCommandFormat => $"That did not parse. Usage: {usage}",
            CommandError.UnrecognizedCommand => "That is not a command.",
            CommandError.ExecutionError => "That failed.",
            _ => $"Usage: {usage}",
        };

    private static string Friendly(Type type)
        => type == typeof(int) || type == typeof(float) || type == typeof(double) ? "a number"
         : type == typeof(bool) ? "true or false"
         : "text";
}
