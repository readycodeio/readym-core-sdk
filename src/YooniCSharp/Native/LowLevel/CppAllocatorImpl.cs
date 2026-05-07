using System.Diagnostics.CodeAnalysis;

namespace Yooni.Native.LowLevel;

// NOTE: This is necessary to prevent code coverage crashes
[ExcludeFromCodeCoverage]
internal static unsafe class CppAllocatorImpl
{
    public static void* AllocMemory(int length)
    {
        return CrtInterop.Malloc((nuint)length);
    }

    public static void FreeMemory(void* ptr)
    {
        CrtInterop.Free(ptr);
    }
}