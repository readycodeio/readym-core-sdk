using System;
using System.Runtime.InteropServices;

namespace ReadyM.Api.Mapping.Events;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct NativeEventCallback
{
    public void* Target;
    public delegate* unmanaged[Cdecl]<void*, IntPtr, void> Callback;

    public bool IsValid
        => Target != null && Callback != null;
}