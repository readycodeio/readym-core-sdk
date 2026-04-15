using System.Runtime.InteropServices;

namespace Yooni.Native.LowLevel;

#if NET8_0_OR_GREATER
internal static unsafe partial class CrtInterop
{
    [LibraryImport("ucrtbase", EntryPoint = "malloc")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void* Malloc(nuint size);

    [LibraryImport("ucrtbase", EntryPoint = "free")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void Free(void* ptr);
}
#else
internal static unsafe class CrtInterop
{
    [DllImport("ucrtbase", EntryPoint = "malloc", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void* Malloc(nuint size);

    [DllImport("ucrtbase", EntryPoint = "free", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Free(void* ptr);
}
#endif