using System.Runtime.InteropServices;

namespace Yooni.Native.LowLevel;

internal static unsafe partial class CrtInterop
{
    [LibraryImport("ucrtbase", EntryPoint = "malloc")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void* Malloc(nuint size);

    [LibraryImport("ucrtbase", EntryPoint = "free")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void Free(void* ptr);
}