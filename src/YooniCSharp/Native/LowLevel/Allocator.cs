using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Yooni.Native.LowLevel;

public static unsafe class Allocator
{
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

        NativeLogging.Logger.LogDebug("[C#] AllocatorKind.{AllocatorKind} Alloc: {Result:X} size {Size} bytes", kind, (long)result, size);
        // NativeLogging.Logger.LogDebug(new StackTrace(true).ToString());
        return result;
    }

    public static void Free(ref void* ptr, AllocatorKind kind)
    {
        NativeLogging.Logger.LogDebug("[C#] AllocatorKind.{AllocatorKind} Free: {Ptr:X}", kind, (long)ptr);
        // NativeLogging.Logger.LogDebug(new StackTrace(true).ToString());

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
}
