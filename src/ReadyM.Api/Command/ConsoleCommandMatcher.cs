using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ReadyM.Api.Command;

internal class ConsoleCommandMatcher(
    ConsoleCommandParser parser,
    ConsoleCommandRegistry registry,
    ConsoleArgumentTypeConverter converter)
{
    public ConsoleCommandRegistry Registry
        => registry;
    
    private static bool CanAssignNull(Type type)
    {
        if (!type.IsValueType)
            return true;
        
        return Nullable.GetUnderlyingType(type) != null;
    }
    
    private bool CanConvertValue(Type destType, object? value)
    {
        if (value == null)
            return CanAssignNull(destType);
        
        if (converter.CanConvert(destType, value))
            return true;

        var nullableUnderlying = Nullable.GetUnderlyingType(destType);
        if (nullableUnderlying != null && converter.CanConvert(nullableUnderlying, value))
            return true;

        return false;
    }

    private object? ConvertValue(Type destType, object? value)
    {
        if (value == null)
        {
            if (!CanAssignNull(destType))
                throw new ArgumentException($"Cannot convert {destType} to {value}.");

            return null;
        }

        if (converter.TryConvert(destType, value, out var result))
            return result;

        var nullableUnderlying = Nullable.GetUnderlyingType(destType);
        if (nullableUnderlying != null && converter.TryConvert(nullableUnderlying, value, out result))
            return result;

        throw new ArgumentException($"Cannot convert {destType} to {value}.");
    }

    private bool TryTypeCheck(ConsoleCommand command, IReadOnlyList<object?> args, [NotNullWhen(false)] out CommandError? error)
    {
        if (args.Count < command.MinArgCount)
        {
            error = new CommandError.TooFewArguments(command.MinArgCount, args.Count);
            return false;
        }
        
        if (args.Count > command.MaxArgCount)
        {
            error = new CommandError.TooManyArguments(command.MaxArgCount.Value, args.Count);
            return false;
        }
        
        var i = 0;
        foreach (var param in command.Parameters)
        {
            if (i < args.Count && args[i] != null && !CanConvertValue(param.ParamType, args[i]!))
            {
                error = new CommandError.InvalidArgumentType(i, param.ParamType, args[i]!.GetType());
                return false;
            }
            
            if (i < args.Count && args[i] == null && !CanAssignNull(param.ParamType))
            {
                error = new CommandError.InvalidArgumentType(i, param.ParamType, typeof(object));
                return false;
            }
            
            if (i >= args.Count && !param.HasDefault)
            {
                error = new CommandError.TooFewArguments(command.MinArgCount, args.Count);
                return false;
            }

            i++;
        }

        if (i < args.Count && command.RepeatingParam == null)
        {
            error = new CommandError.TooManyArguments(command.MaxArgCount!.Value, args.Count);
            return false;
        }
        
        if (command.RepeatingParam != null)
        {
            // NOTE: Handling params ...
            for (var j = i; j < args.Count; j++)
            {
                var arg = args[j];
                
                if (arg != null && !CanConvertValue(command.RepeatingParam, arg))
                {
                    error = new CommandError.InvalidArgumentType(j, command.RepeatingParam, arg.GetType());
                    return false;
                }
                
                if (arg == null && !CanAssignNull(command.RepeatingParam))
                {
                    error = new CommandError.InvalidArgumentType(j, command.RepeatingParam, typeof(object));
                    return false;
                }
            }
        }

        error = null;
        return true;
    }

    private bool TryConvert(ConsoleCommand command, IReadOnlyList<object?> args, [NotNullWhen(true)] out IReadOnlyList<object?>? actualArgs, [NotNullWhen(false)] out CommandError? error)
    {
        if (!TryTypeCheck(command, args, out error))
        {
            actualArgs = null;
            return false;
        }
        
        var result = new List<object?>();

        var i = 0;
        foreach (var param in command.Parameters)
        {
            if (i < args.Count)
            {
                var actualArg = ConvertValue(param.ParamType, args[i]);
                result.Add(actualArg);
                i++;
                continue;
            }
            
            if (param.HasDefault)
            {
                var actualArg = ConvertValue(param.ParamType, param.DefaultValue);
                result.Add(actualArg);
                i++;
                continue;
            }

            throw new InvalidOperationException();
        }
        
        if (command.RepeatingParam != null)
        {
            // NOTE: Handling params ...
            var repeatingArg = Array.CreateInstance(command.RepeatingParam, args.Count - i);

            for (var j = i; j < args.Count; j++)
            {
                var arg = args[j];
                var actualArg = ConvertValue(command.RepeatingParam, arg);
                repeatingArg.SetValue(actualArg, j - i);
            }
            
            result.Add(repeatingArg);
        }

        actualArgs = result;
        error = null;
        return true;
    }
    
    public bool TryMatch(string input, [NotNullWhen(true)] out ConsoleCommand? command, [NotNullWhen(true)] out ParsedCommandCall? commandCall, [NotNullWhen(false)] out CommandError? error)
    {
        if (!parser.TryParse(input, out commandCall, out error))
        {
            command = null;
            commandCall = null;
            return false;
        }

        if (!registry.HasCommand(commandCall.Value.CommandName))
        {
            error = new CommandError.UnrecognizedCommand(commandCall.Value.CommandName);
            command = null;
            commandCall = null;
            return false;
        }
        
        command = registry.GetCommand(commandCall.Value.CommandName);

        if (!TryConvert(command.Value, commandCall.Value.Args, out var actualArgs, out error))
        {
            command = null;
            commandCall = null;
            return false;
        }
        
        commandCall = new ParsedCommandCall(commandCall.Value.CommandName, actualArgs);
        return true;
    }
}