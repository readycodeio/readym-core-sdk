using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container.Tests;

public static unsafe class NativeListProxy<T>
    where T : unmanaged
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DisposeArgs
    {
        public void* TargetPtr;
    }

    public static int Dispose(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(DisposeArgs))
        {
            Console.WriteLine($"Invalid argument size for Dispose: expected {sizeof(DisposeArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<DisposeArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
        target.Dispose();
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IsCreatedArgs
    {
        public void* TargetPtr;
        public byte Result;
    }

    public static int IsCreated(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(IsCreatedArgs))
        {
            Console.WriteLine($"Invalid argument size for IsCreated: expected {sizeof(IsCreatedArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<IsCreatedArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
        values.Result = (byte)(target.IsCreated ? 1 : 0);
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
            Console.WriteLine($"Invalid argument size for GetCount: expected {sizeof(GetCountArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<GetCountArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
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
            Console.WriteLine($"Invalid argument size for GetCapacity: expected {sizeof(GetCapacityArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<GetCapacityArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
        values.Result = target.Capacity;
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
            Console.WriteLine($"Invalid argument size for GetItem: expected {sizeof(GetItemArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<GetItemArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
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
            Console.WriteLine($"Invalid argument size for SetItem: expected {sizeof(SetItemArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<SetItemArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
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
            Console.WriteLine($"Invalid argument size for Add: expected {sizeof(AddArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<AddArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
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
            Console.WriteLine($"Invalid argument size for Insert: expected {sizeof(InsertArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<InsertArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
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
            Console.WriteLine($"Invalid argument size for InsertRangeValue: expected {sizeof(InsertRangeValueArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<InsertRangeValueArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
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
            Console.WriteLine($"Invalid argument size for InsertRangeList: expected {sizeof(InsertRangeListArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<InsertRangeListArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
        ref var source = ref Unsafe.AsRef<NativeList<T>>(values.SourcePtr);
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
            Console.WriteLine($"Invalid argument size for RemoveAt: expected {sizeof(RemoveAtArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<RemoveAtArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
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
            Console.WriteLine($"Invalid argument size for RemoveSwapBack: expected {sizeof(RemoveSwapBackArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<RemoveSwapBackArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
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
            Console.WriteLine($"Invalid argument size for RemoveRange: expected {sizeof(RemoveRangeArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<RemoveRangeArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
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
            Console.WriteLine($"Invalid argument size for Clear: expected {sizeof(ClearArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<ClearArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
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
            Console.WriteLine($"Invalid argument size for EnsureLength: expected {sizeof(EnsureLengthArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<EnsureLengthArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
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
            Console.WriteLine($"Invalid argument size for Resize: expected {sizeof(ResizeArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<ResizeArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
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
            Console.WriteLine($"Invalid argument size for ZeroMemory: expected {sizeof(ZeroMemoryArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<ZeroMemoryArgs>();
        ref var target = ref Unsafe.AsRef<NativeList<T>>(values.TargetPtr);
        target.ZeroMemory(values.Index, values.Count);
        return 0;
    }
}