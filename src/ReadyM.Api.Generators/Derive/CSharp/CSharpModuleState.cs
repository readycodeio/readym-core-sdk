using System.Collections.Generic;

namespace ReadyM.Api.Generators.Derive.CSharp;

internal class CSharpModuleState()
{
    private readonly List<string> _usings = [];
    
    public IReadOnlyList<string> Usings => _usings;
    
    public void AddUsing(string ns)
    {
        if (!_usings.Contains(ns))
            _usings.Add(ns);
    }

    public void AddUsingList(List<string> list)
    {
        foreach (var ns in list)
        {
            AddUsing(ns);
        }
    }
}