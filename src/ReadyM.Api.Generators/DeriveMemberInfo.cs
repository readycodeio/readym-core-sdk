using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

internal class DeriveMemberInfo(
    ISymbol symbol,
    string name,
    ITypeSymbol type,
    int order,
    bool readOnly,
    IReadOnlyList<string> errors)
{
    public ISymbol Symbol { get; } = symbol;
    public string Name { get; } = name;
    public ITypeSymbol Type { get; } = type;
    public int Order { get; } = order;
    public bool ReadOnly { get; } = readOnly;
    
    private readonly List<string> _errors = [..errors];

    public bool HasErrors
        => _errors.Count > 0;
    
    public IReadOnlyList<string> Errors
        => _errors;
    
    public void AddError(string error)
    {
        _errors.Add(error);
    }
}