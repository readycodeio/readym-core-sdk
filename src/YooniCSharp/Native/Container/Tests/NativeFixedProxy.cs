using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Yooni.Native.Logging;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container.Tests;

public static unsafe class NativeFixedProxy<T, TStorage>
    where T : unmanaged
    where TStorage : unmanaged, IStorage<T>
{
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
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
        values.Result = target.Capacity;
        return 0;
    }

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
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
        values.Result = target.Count;
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
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
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
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
        target[values.Index] = values.Value;
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AddArgs
    {
        public void* TargetPtr;
        public T Value;
        public int Result;
    }

    public static int Add(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(AddArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for Add: expected {Args}, got {SizeBytes}", sizeof(AddArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<AddArgs>();
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
        values.Result = target.Add(values.Value);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InsertArgs
    {
        public void* TargetPtr;
        public int Index;
        public T Value;
    }

    public static int Insert(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(InsertArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for Insert: expected {Args}, got {SizeBytes}", sizeof(InsertArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<InsertArgs>();
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
        target.Insert(values.Index, values.Value);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InsertRangeValueArgs
    {
        public void* TargetPtr;
        public int Index;
        public T Value;
        public int Count;
    }

    public static int InsertRangeValue(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(InsertRangeValueArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for InsertRangeValue: expected {Args}, got {SizeBytes}", sizeof(InsertRangeValueArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<InsertRangeValueArgs>();
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
        target.InsertRange(values.Index, values.Value, values.Count);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InsertRangeListArgs
    {
        public void* TargetPtr;
        public int Index;
        public void* SourcePtr;
    }

    public static int InsertRangeList(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(InsertRangeListArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for InsertRangeList: expected {Args}, got {SizeBytes}", sizeof(InsertRangeListArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<InsertRangeListArgs>();
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
        ref var source = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.SourcePtr);
        target.InsertRange(values.Index, source);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RemoveAtArgs
    {
        public void* TargetPtr;
        public int Index;
        public T Result;
    }

    public static int RemoveAt(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(RemoveAtArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for RemoveAt: expected {Args}, got {SizeBytes}", sizeof(RemoveAtArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<RemoveAtArgs>();
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
        values.Result = target.RemoveAt(values.Index);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RemoveSwapBackArgs
    {
        public void* TargetPtr;
        public int Index;
        public T Result;
    }

    public static int RemoveSwapBack(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(RemoveSwapBackArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for RemoveSwapBack: expected {Args}, got {SizeBytes}", sizeof(RemoveSwapBackArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<RemoveSwapBackArgs>();
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
        values.Result = target.RemoveSwapBack(values.Index);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RemoveRangeArgs
    {
        public void* TargetPtr;
        public int Index;
        public int Count;
    }

    public static int RemoveRange(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(RemoveRangeArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for RemoveRange: expected {Args}, got {SizeBytes}", sizeof(RemoveRangeArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<RemoveRangeArgs>();
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
        target.RemoveRange(values.Index, values.Count);
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
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
        target.Clear();
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EnsureLengthArgs
    {
        public void* TargetPtr;
        public int TargetLength;
        public byte Result;
    }

    public static int EnsureLength(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(EnsureLengthArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for EnsureLength: expected {Args}, got {SizeBytes}", sizeof(EnsureLengthArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<EnsureLengthArgs>();
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
        values.Result = (byte)(target.EnsureLength(values.TargetLength) ? 1 : 0);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ResizeArgs
    {
        public void* TargetPtr;
        public int NewLength;
    }

    public static int Resize(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(ResizeArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for Resize: expected {Args}, got {SizeBytes}", sizeof(ResizeArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<ResizeArgs>();
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
        target.Resize(values.NewLength);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ZeroMemoryArgs
    {
        public void* TargetPtr;
        public int Index;
        public int Count;
    }

    public static int ZeroMemory(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(ZeroMemoryArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for ZeroMemory: expected {Args}, got {SizeBytes}", sizeof(ZeroMemoryArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<ZeroMemoryArgs>();
        ref var target = ref Unsafe.AsRef<NativeFixed<T, TStorage>>(values.TargetPtr);
        target.ZeroMemory(values.Index, values.Count);
        return 0;
    }
}
