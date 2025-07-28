using System;
using ReadyM.Api.Systems;

namespace ReadyM.Api;

public abstract class PatcherBase : IDisposable
{
    public bool IsDisposed { get; private set; }
    public bool IsPatched { get; private set; }
    
    public void Patch()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(SystemUpdateLoop), "Mod is already disposed.");
        if (IsPatched)
            throw new InvalidOperationException("Mod is already patched.");
        IsPatched = true;
        OnPatch();
    }
    
    protected virtual void OnPatch()
    {
        // empty
    }
    
    public void Unpatch()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(SystemUpdateLoop), "Mod is already disposed.");
        if (!IsPatched)
            throw new InvalidOperationException("Mod is not patched.");
        IsPatched = false;
        OnUnpatch();
    }

    protected virtual void OnUnpatch()
    {
        // empty
    }

    public void Dispose()
    {
        if (IsPatched)
            Unpatch();
        IsDisposed = true;
    }
}