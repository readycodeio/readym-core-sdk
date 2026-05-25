using System;
using System.Runtime.InteropServices;

namespace ReadyM.Api.Mapping.Events;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeEventCallback
{
    private delegate* unmanaged[Cdecl]<IntPtr, void*, void> Functor;
    private void* Context;

    public bool IsValid => Functor != null && Context != null;
    
    public void Invoke(IntPtr eventData)
    {
        if (!IsValid)
            throw new InvalidOperationException("Invalid NativeEventCallback: Functor and Context must be non-null.");

        Functor(eventData, Context);
    }
}