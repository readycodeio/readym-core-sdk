using System;
using System.Collections.Generic;

namespace ReadyM.Api.Generators.Derive.CSharp;

internal class CSharpMethodState(CSharpClassState classState)
{
    public readonly CSharpClassState ClassState = classState;
    public CSharpModuleState ModuleState => ClassState.ModuleState;

    private readonly Dictionary<string, int> _varCounters = [];

    public string NewVarName(string name)
    {
        if (!_varCounters.TryGetValue(name, out var index))
        {
            index = 0;
            _varCounters.Add(name, 0);
        }

        _varCounters[name] = index + 1;
        return name + index;
    }
}