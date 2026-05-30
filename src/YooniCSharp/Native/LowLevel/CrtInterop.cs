using System.Runtime.InteropServices;

namespace Yooni.Native.LowLevel;

#if NET8_0_OR_GREATER
internal static unsafe partial class CrtInterop
{
#if WINDOWS || _WINDOWS
    private const string CLib = "ucrtbase";
#else
    private const string CLib = "libc";
#endif

    [LibraryImport(CLib, EntryPoint = "malloc")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void* Malloc(nuint size);

    [LibraryImport(CLib, EntryPoint = "free")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void Free(void* ptr);
}
#else
internal static unsafe class CrtInterop
{
#if WINDOWS || _WINDOWS
    private const string CLib = "ucrtbase";
#else
    private const string CLib = "libc";
#endif

    [DllImport(CLib, EntryPoint = "malloc", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void* Malloc(nuint size);

    [DllImport(CLib, EntryPoint = "free", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Free(void* ptr);
}
#endif