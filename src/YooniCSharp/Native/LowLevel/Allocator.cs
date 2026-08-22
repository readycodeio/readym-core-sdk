using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Yooni.Native.Logging;

namespace Yooni.Native.LowLevel;

public static unsafe class Allocator
{
    private static NativeLogLevel _level = NativeLogLevel.Disabled;

    public static void* Alloc(int size, AllocatorKind kind)
    {
        void* result;
        switch (kind)
        {
            case AllocatorKind.InternalCall:
                result = InternalCallAllocatorImpl.AllocMemory(size);
                break;
            case AllocatorKind.NativeUnity:
                result = NativeUnityAllocatorImpl.AllocMemory(size);
                break;
            case AllocatorKind.Marshal:
                result = MarshalAllocatorImpl.AllocMemory(size);
                break;
            case AllocatorKind.Cpp:
                result = CppAllocatorImpl.AllocMemory(size);
                break;
            default:
                throw new InvalidOperationException($"Invalid allocator kind: {kind}");
        }

        switch (_level)
        {
            case NativeLogLevel.Disabled:
                break;
            case NativeLogLevel.Enabled:
                NativeLogging.Logger.LogDebug("ALLOC 0x{Result:x} AllocatorKind.{AllocatorKind} size {Size} bytes", kind, (long)result, size);
                break;
            case NativeLogLevel.EnableStacktrace:
                NativeLogging.Logger.LogDebug("ALLOC 0x{Result:x} AllocatorKind.{AllocatorKind} size {Size} bytes", kind, (long)result, size);
                NativeLogging.Logger.LogDebug(new StackTrace(true).ToString());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return result;
    }

    public static void Free(ref void* ptr, AllocatorKind kind)
    {
        switch (_level)
        {
            case NativeLogLevel.Disabled:
                break;
            case NativeLogLevel.Enabled:
                NativeLogging.Logger.LogDebug("[C#] FREE 0x{Ptr:x} AllocatorKind.{AllocatorKind}", kind, (long)ptr);
                break;
            case NativeLogLevel.EnableStacktrace:
                NativeLogging.Logger.LogDebug("[C#] FREE 0x{Ptr:x} AllocatorKind.{AllocatorKind}", kind, (long)ptr);
                NativeLogging.Logger.LogDebug(new StackTrace(true).ToString());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        switch (kind)
        {
            case AllocatorKind.InternalCall:
                InternalCallAllocatorImpl.FreeMemory(ptr);
                break;
            case AllocatorKind.NativeUnity:
                NativeUnityAllocatorImpl.FreeMemory(ptr);
                break;
            case AllocatorKind.Marshal:
                MarshalAllocatorImpl.FreeMemory(ptr);
                break;
            case AllocatorKind.Cpp:
                CppAllocatorImpl.FreeMemory(ptr);
                break;
            default:
                throw new InvalidOperationException($"Invalid allocator kind: {kind}");
        }

        ptr = null;
    }

    public static void SetLogging(NativeLogLevel level)
    {
        _level = level;
    }
}
