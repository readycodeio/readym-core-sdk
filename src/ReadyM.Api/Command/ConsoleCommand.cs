using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ReadyM.Api.Command;

public readonly struct ConsoleCommand(
    Delegate handler,
    int minArgCount,
    int? maxArgCount,
    IReadOnlyList<ConsoleCommand.ParamInfo> args,
    Type? repeatingParam,
    bool isDebugOnly)
{
    public readonly struct ParamInfo(Type paramType, bool hasDefault, object? defaultValue)
    {
        public readonly Type ParamType = paramType;
        public readonly bool HasDefault = hasDefault;
        public readonly object? DefaultValue = defaultValue;
    }
    
    public readonly Delegate Handler = handler;
    public readonly int MinArgCount = minArgCount;
    public readonly int? MaxArgCount = maxArgCount;
    public readonly IReadOnlyList<ParamInfo> Parameters = args;
    public readonly Type? RepeatingParam = repeatingParam;
    public readonly bool IsDebugOnly = isDebugOnly;

    public static ConsoleCommand Create(Delegate handler, bool isDebugOnly)
    {
        var minArgCount = 0;
        int? maxArgCount = 0;
        var parameters = new List<ParamInfo>();
        Type? repeatingParam = null;

        var i = 0;
        var inputParameters = handler.Method.GetParameters();
        foreach (var param in inputParameters)
        {
            if (Attribute.IsDefined(param, typeof(ParamArrayAttribute)))
            {
                Debug.Assert(i == inputParameters.Length - 1, "i == inputParameters.Length - 1");
                repeatingParam = param.ParameterType.GetElementType();
                maxArgCount = null;
                break;
            }
            
            parameters.Add(new ParamInfo(
                param.ParameterType,
                param.IsOptional,
                param.IsOptional ? param.DefaultValue : null));

            if (!param.IsOptional)
                minArgCount++;
            maxArgCount++;
        }
        
        return new ConsoleCommand(handler, minArgCount, maxArgCount, parameters, repeatingParam, isDebugOnly);
    }

    public void Invoke(IReadOnlyList<object?> args)
        => Handler?.DynamicInvoke(args.ToArray());
}
