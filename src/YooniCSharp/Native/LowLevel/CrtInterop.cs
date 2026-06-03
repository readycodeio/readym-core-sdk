using System.Runtime.InteropServices;

namespace Yooni.Native.LowLevel;

internal static unsafe class CrtInterop
{
    private static readonly bool _isWindows =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    internal static void* Malloc(nuint size) =>
        _isWindows ? Windows.Malloc(size) : Unix.Malloc(size);

    internal static void Free(void* ptr)
    {
        if (_isWindows) Windows.Free(ptr);
        else Unix.Free(ptr);
    }

    private static class Windows
    {
        [DllImport("ucrtbase", EntryPoint = "malloc", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void* Malloc(nuint size);

        [DllImport("ucrtbase", EntryPoint = "free", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Free(void* ptr);
    }

    private static class Unix
    {
        [DllImport("libc", EntryPoint = "malloc", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void* Malloc(nuint size);

        [DllImport("libc", EntryPoint = "free", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Free(void* ptr);
    }
}