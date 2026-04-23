using System.Runtime.CompilerServices;

namespace Yooni.Native.LowLevel;

public static unsafe class MemoryUtils
{
    public static void ZeroMemory(byte* ptr, int length)
        => Unsafe.InitBlock(ref *ptr, 0, (uint)length);

    public static void CopyMemory(byte* destPtr, byte* sourcePtr, int length)
        => Unsafe.CopyBlock(ref *destPtr, ref *sourcePtr, (uint)length);
}