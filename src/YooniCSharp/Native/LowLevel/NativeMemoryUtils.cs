using System.Runtime.CompilerServices;

namespace Yooni.Native.LowLevel;

public static unsafe class NativeMemoryUtils
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void* Malloc(int length);

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void Free(void* ptr);

    // This method wraps Malloc in order to get rid of SecurityException: ECall methods must be packaged into a system module.
    public static void* AllocMemory(int length)
        => Malloc(length);

    // This method wraps Free in order to get rid of SecurityException: ECall methods must be packaged into a system module.
    public static void FreeMemory(void* ptr)
        => Free(ptr);

    public static void ZeroMemory(byte* ptr, int length)
        => Unsafe.InitBlock(ref *ptr, 0, (uint)length);

    public static void CopyMemory(byte* destination, byte* source, int length)
        => Unsafe.CopyBlock(ref *destination, ref *source, (uint)length);
}