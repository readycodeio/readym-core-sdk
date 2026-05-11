using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive;

internal class DeriveTypeSupportVisitorBase<TImpl>(IReadOnlyList<TImpl> impls, TImpl? fallbackImpl)
    where TImpl : IDeriveTypeSupportImplBase
{
    private readonly List<TImpl> _impls = [..impls];

    public bool TryGetImpl(ITypeSymbol symbol, bool fallback, [NotNullWhen(true)] out TImpl? result)
    {
        foreach (var impl in _impls)
        {
            if (impl.Supports(symbol))
            {
                result = impl;
                return true;
            }
        }

        if (fallback && fallbackImpl != null)
        {
            result = fallbackImpl;
            return true;
        }
        
        result = default;
        return false;
    }
    
    public TImpl GetImpl(ITypeSymbol symbol, bool fallback)
    {
        if (TryGetImpl(symbol, fallback, out var impl))
            return impl;
        
        throw new InvalidOperationException($"No derive type support implementation found for symbol {symbol.ToDisplayString()}");
    }
}