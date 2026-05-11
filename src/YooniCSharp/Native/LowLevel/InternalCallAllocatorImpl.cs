using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Yooni.Native.LowLevel;

// NOTE: This is necessary to prevent code coverage crashes
[ExcludeFromCodeCoverage]
internal static unsafe class InternalCallAllocatorImpl
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void* MallocInternal(int length);

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void FreeInternal(void* ptr);
    
    // This method wraps Malloc in order to get rid of SecurityException: ECall methods must be packaged into a system module.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void* AllocMemory(int length)
        => MallocInternal(length);

    // This method wraps Free in order to get rid of SecurityException: ECall methods must be packaged into a system module.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void FreeMemory(void* ptr)
        => FreeInternal(ptr);
}