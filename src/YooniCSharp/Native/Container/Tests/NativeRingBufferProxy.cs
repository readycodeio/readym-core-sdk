using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container.Tests;

public static unsafe class NativeRingBufferProxy<T, TStorage>
    where T : unmanaged
    where TStorage : unmanaged, IStorage<T>
{
    [StructLayout(LayoutKind.Sequential)]
    private struct GetCountArgs
    {
        public void* TargetPtr;
        public int Result;
    }

    public static int GetCount(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(GetCountArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for GetCount: expected {Args}, got {SizeBytes}", sizeof(GetCountArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<GetCountArgs>();
        ref var target = ref Unsafe.AsRef<NativeRingBuffer<T, TStorage>>(values.TargetPtr);
        values.Result = target.Count;
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GetCapacityArgs
    {
        public void* TargetPtr;
        public int Result;
    }

    public static int GetCapacity(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(GetCapacityArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for GetCapacity: expected {Args}, got {SizeBytes}", sizeof(GetCapacityArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<GetCapacityArgs>();
        ref var target = ref Unsafe.AsRef<NativeRingBuffer<T, TStorage>>(values.TargetPtr);
        values.Result = target.Capacity;
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ClearArgs
    {
        public void* TargetPtr;
    }

    public static int Clear(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(ClearArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for Clear: expected {Args}, got {SizeBytes}", sizeof(ClearArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<ClearArgs>();
        ref var target = ref Unsafe.AsRef<NativeRingBuffer<T, TStorage>>(values.TargetPtr);
        target.Clear();
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PushArgs
    {
        public void* TargetPtr;
        public T Value;
        public byte Result;
    }

    public static int Push(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(PushArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for Push: expected {Args}, got {SizeBytes}", sizeof(PushArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<PushArgs>();
        ref var target = ref Unsafe.AsRef<NativeRingBuffer<T, TStorage>>(values.TargetPtr);
        values.Result = (byte)(target.Push(values.Value) ? 1 : 0);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PopArgs
    {
        public void* TargetPtr;
    }

    public static int Pop(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(PopArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for Pop: expected {Args}, got {SizeBytes}", sizeof(PopArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<PopArgs>();
        ref var target = ref Unsafe.AsRef<NativeRingBuffer<T, TStorage>>(values.TargetPtr);
        target.Pop();
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GetItemArgs
    {
        public void* TargetPtr;
        public int Index;
        public T Result;
    }

    public static int GetItem(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(GetItemArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for GetItem: expected {Args}, got {SizeBytes}", sizeof(GetItemArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<GetItemArgs>();
        ref var target = ref Unsafe.AsRef<NativeRingBuffer<T, TStorage>>(values.TargetPtr);
        values.Result = target[values.Index];
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SetItemArgs
    {
        public void* TargetPtr;
        public int Index;
        public T Value;
    }

    public static int SetItem(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(SetItemArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for SetItem: expected {Args}, got {SizeBytes}", sizeof(SetItemArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<SetItemArgs>();
        ref var target = ref Unsafe.AsRef<NativeRingBuffer<T, TStorage>>(values.TargetPtr);
        target[values.Index] = values.Value;
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GetNewestArgs
    {
        public void* TargetPtr;
        public T Result;
    }

    public static int GetNewest(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(GetNewestArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for GetNewest: expected {Args}, got {SizeBytes}", sizeof(GetNewestArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<GetNewestArgs>();
        ref var target = ref Unsafe.AsRef<NativeRingBuffer<T, TStorage>>(values.TargetPtr);
        values.Result = target.Newest;
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SetNewestArgs
    {
        public void* TargetPtr;
        public T Value;
    }

    public static int SetNewest(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(SetNewestArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for SetNewest: expected {Args}, got {SizeBytes}", sizeof(SetNewestArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<SetNewestArgs>();
        ref var target = ref Unsafe.AsRef<NativeRingBuffer<T, TStorage>>(values.TargetPtr);
        target.Newest = values.Value;
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GetOldestArgs
    {
        public void* TargetPtr;
        public T Result;
    }

    public static int GetOldest(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(GetOldestArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for GetOldest: expected {Args}, got {SizeBytes}", sizeof(GetOldestArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<GetOldestArgs>();
        ref var target = ref Unsafe.AsRef<NativeRingBuffer<T, TStorage>>(values.TargetPtr);
        values.Result = target.Oldest;
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SetOldestArgs
    {
        public void* TargetPtr;
        public T Value;
    }

    public static int SetOldest(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(SetOldestArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for SetOldest: expected {Args}, got {SizeBytes}", sizeof(SetOldestArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<SetOldestArgs>();
        ref var target = ref Unsafe.AsRef<NativeRingBuffer<T, TStorage>>(values.TargetPtr);
        target.Oldest = values.Value;
        return 0;
    }
}
