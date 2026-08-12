using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ReadyM.Api.Interop;

internal class PinnedDelegateStore : IDisposable
{
    private readonly Dictionary<Delegate, GCHandle> _pinnedDelegates = new();

    public IntPtr PinDelegate<TDelegate>(TDelegate del)
        where TDelegate : Delegate
    {
        if (!_pinnedDelegates.TryGetValue(del, out var handle))
        {
            handle = GCHandle.Alloc(del);
            _pinnedDelegates.Add(del, handle);
        }

        return Marshal.GetFunctionPointerForDelegate(del);
    }

    public void UnpinDelegate<TDelegate>(TDelegate del)
        where TDelegate : Delegate
    {
        if (_pinnedDelegates.TryGetValue(del, out var handle))
        {
            handle.Free();
            _pinnedDelegates.Remove(del);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (var handle in _pinnedDelegates)
        {
            handle.Value.Free();
        }

        _pinnedDelegates.Clear();
    }

    ~PinnedDelegateStore()
    {
        Console.WriteLine("Warning: PinnedDelegateStore was not disposed properly. This may lead to memory leaks.");
        Dispose();
    }
}