using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yooni.Native.LowLevel;

internal static unsafe class MarshalAllocatorImpl
{
    // This method wraps Malloc in order to get rid of SecurityException: ECall methods must be packaged into a system module.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void* AllocMemory(int length)
        => (void*)Marshal.AllocHGlobal(length);

    // This method wraps Free in order to get rid of SecurityException: ECall methods must be packaged into a system module.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void FreeMemory(void* ptr)
        => Marshal.FreeHGlobal((IntPtr)ptr);
}