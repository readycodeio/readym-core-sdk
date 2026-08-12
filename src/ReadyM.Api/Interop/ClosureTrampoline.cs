using System;
using System.Runtime.InteropServices;

namespace ReadyM.Api.Interop;

/// A handle for a C++ closure that does not take any parameters.
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ClosureTrampoline0
{
    private delegate* unmanaged[Cdecl]<void*, void> Functor;
    private void* Context;

    /// Invokes the closure.
    /// <exception cref="NullReferenceException">When the closure function or context pointers are invalid.</exception>
    public void Invoke()
    {
        if (Functor is null || Context is null)
            throw new NullReferenceException("Invalid NativeEventCallback: Functor and Context must be non-null.");

        Functor(Context);
    }
}

/// A handle for a C++ closure that takes a single parameter.
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ClosureTrampoline1
{
    private delegate* unmanaged[Cdecl]<void*, IntPtr, void> Functor;
    private void* Context;

    /// Invokes the closure.
    /// <exception cref="NullReferenceException">When the closure function or context pointers are invalid.</exception>
    public void Invoke(IntPtr eventData)
    {
        if (Functor is null || Context is null)
            throw new NullReferenceException("Invalid NativeEventCallback: Functor and Context must be non-null.");

        Functor(Context, eventData);
    }
}

// ADD MORE TRAMPOLINES HERE AS NEEDED, FOLLOWING THE SAME PATTERN.