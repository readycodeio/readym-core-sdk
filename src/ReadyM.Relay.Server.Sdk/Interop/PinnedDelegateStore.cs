using System.Runtime.InteropServices;

namespace ReadyM.Relay.Server.Sdk.Interop;

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

    public void Dispose()
    {
        foreach (var handle in _pinnedDelegates)
        {
            handle.Value.Free();
        }

        _pinnedDelegates.Clear();
    }
}