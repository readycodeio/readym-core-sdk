using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive;

internal abstract class DeriveSupportVisitorBase<TItem, TImpl>(IReadOnlyList<TImpl> impls, TImpl? fallbackImpl)
    where TImpl : IDeriveSupportImplBase<TItem>
{
    private readonly List<TImpl> _impls = [..impls];

    protected abstract string ToDisplayString(TItem item); 
    
    public bool TryGetImpl(TItem item, bool fallback, [NotNullWhen(true)] out TImpl? result)
    {
        foreach (var impl in _impls)
        {
            if (impl.Supports(item))
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
    
    public TImpl GetImpl(TItem item, bool fallback)
    {
        if (TryGetImpl(item, fallback, out var impl))
            return impl;
        
        throw new InvalidOperationException($"No derive type support implementation found for symbol {ToDisplayString(item)}");
    }
}