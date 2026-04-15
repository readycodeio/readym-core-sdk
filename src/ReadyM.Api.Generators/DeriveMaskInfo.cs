using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

internal sealed class DeriveMaskInfo(ITypeSymbol type, int bits, bool invalid)
{
    public ITypeSymbol Type { get; } = type ?? throw new ArgumentNullException(nameof(type));
    public int Bits { get; } = bits;

    private readonly List<string> _errors = [];
    
    public IReadOnlyList<string> Errors
        => _errors;
    
    public bool HasErrors
        => _errors.Count > 0;
    
    public void AddError(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(error));
        
        _errors.Add(error);
    }
}