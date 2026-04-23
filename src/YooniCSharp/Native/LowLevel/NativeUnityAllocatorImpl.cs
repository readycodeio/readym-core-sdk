
using System.Runtime.CompilerServices;
#if UNITY_EDITOR || UNITY_STANDALONE
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Yooni.Native.LowLevel;

internal static unsafe class NativeUnityAllocatorImpl
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void* AllocMemory(int length)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return UnsafeUtility.Malloc(length, 8, Unity.Collections.Allocator.Persistent);
#else
        throw new System.NotSupportedException();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void FreeMemory(void* ptr)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        UnsafeUtility.Free(ptr, Unity.Collections.Allocator.Persistent);
#else
        throw new System.NotSupportedException();
#endif
    }
}