using System;

namespace ReadyM.Api;

public abstract class PatcherBase : IDisposable
{
    public bool IsDisposed { get; private set; }
    public bool IsPatched { get; private set; }
    
    public void Patch()
    {
        if (IsDisposed)
            throw new ObjectDisposedException("Mod is already disposed.");
        if (IsPatched)
            throw new InvalidOperationException("Mod is already patched.");
        IsPatched = true;
        OnPatch();
        OnCommit();
    }
    
    protected virtual void OnPatch()
    {
        // empty
    }

    protected abstract void OnCommit();
    
    public void Unpatch()
    {
        if (IsDisposed)
            throw new ObjectDisposedException("Mod is already disposed.");
        if (!IsPatched)
            throw new InvalidOperationException("Mod is not patched.");
        IsPatched = false;
        OnUnpatch();
        OnCommit();
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