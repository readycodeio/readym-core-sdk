using System;
using System.Collections.Generic;
using ReadyM.Api.Command;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Commands;

internal sealed class CommandBinding
{
    private CommandBinding(int? remainderIndex, ConsoleCommand console)
    {
        RemainderIndex = remainderIndex;
        Console = console;
    }

    // Which argument takes the rest of the line, counted without the leading context, or null.
    public int? RemainderIndex { get; }

    internal ConsoleCommand Console { get; }

    public static CommandBinding Of(Delegate handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var parameters = handler.Method.GetParameters();

        if (parameters.Length == 0 || parameters[0].ParameterType != typeof(CommandContext))
        {
            throw new ArgumentException(
                $"A command handler takes {nameof(CommandContext)} first, and {handler.Method.Name} does not.",
                nameof(handler));
        }

        int? remainderIndex = null;
        var infos = new List<ConsoleCommand.ParamInfo>();
        Type? repeating = null;
        var min = 0;
        int? max = 0;

        for (var i = 1; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            if (parameter.IsDefined(typeof(RemainderAttribute), inherit: false))
            {
                if (i != parameters.Length - 1 || parameter.ParameterType != typeof(string))
                {
                    throw new ArgumentException(
                        $"Remainder goes on the last parameter of {handler.Method.Name} and it must be a string.",
                        nameof(handler));
                }

                remainderIndex = i - 1;
            }

            if (parameter.IsDefined(typeof(ParamArrayAttribute), inherit: false))
            {
                repeating = parameter.ParameterType.GetElementType();
                max = null;
                break;
            }

            infos.Add(new ConsoleCommand.ParamInfo(
                parameter.ParameterType, parameter.HasDefaultValue, parameter.DefaultValue));

            if (!parameter.HasDefaultValue) min++;
            if (max.HasValue) max++;
        }

        return new CommandBinding(remainderIndex, new ConsoleCommand(handler, min, max, infos, repeating, false));
    }
}
