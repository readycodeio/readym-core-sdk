using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Yooni.Native.Logging;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container.Tests;

public static unsafe class NativeHashCollectionProxy<TKey, TValue>
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
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
            NativeLogging.Logger.LogError("Invalid argument size for Dispose: expected {Args}, got {SizeBytes}", sizeof(DisposeArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<DisposeArgs>();
        ref var target = ref Unsafe.AsRef<NativeHashCollection<TKey, TValue>>(values.TargetPtr);
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
            NativeLogging.Logger.LogError("Invalid argument size for IsCreated: expected {Args}, got {SizeBytes}", sizeof(IsCreatedArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<IsCreatedArgs>();
        ref var target = ref Unsafe.AsRef<NativeHashCollection<TKey, TValue>>(values.TargetPtr);
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
            NativeLogging.Logger.LogError("Invalid argument size for GetCount: expected {Args}, got {SizeBytes}", sizeof(GetCountArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<GetCountArgs>();
        ref var target = ref Unsafe.AsRef<NativeHashCollection<TKey, TValue>>(values.TargetPtr);
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
        ref var target = ref Unsafe.AsRef<NativeHashCollection<TKey, TValue>>(values.TargetPtr);
        values.Result = target.Capacity;
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InsertArgs
    {
        public void* TargetPtr;
        public TKey Key;
        public uint ValueHash;
        public TValue Value;
        public void* Result;
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
        ref var target = ref Unsafe.AsRef<NativeHashCollection<TKey, TValue>>(values.TargetPtr);
        values.Result = target.Insert(values.Key, values.ValueHash, values.Value).GetPointer();
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RemoveArgs
    {
        public void* TargetPtr;
        public TKey Key;
        public uint ValueHash;
        public byte Result;
    }

    public static int Remove(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(RemoveArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for Remove: expected {Args}, got {SizeBytes}", sizeof(RemoveArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<RemoveArgs>();
        ref var target = ref Unsafe.AsRef<NativeHashCollection<TKey, TValue>>(values.TargetPtr);
        values.Result = (byte)(target.Remove(values.Key, values.ValueHash) ? 1 : 0);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FindArgs
    {
        public void* TargetPtr;
        public TKey Key;
        public uint ValueHash;
        public void* Result;
    }

    public static int Find(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(FindArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for Find: expected {Args}, got {SizeBytes}", sizeof(FindArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<FindArgs>();
        ref var target = ref Unsafe.AsRef<NativeHashCollection<TKey, TValue>>(values.TargetPtr);
        values.Result = target.Find(values.Key, values.ValueHash).GetPointer();
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
        ref var target = ref Unsafe.AsRef<NativeHashCollection<TKey, TValue>>(values.TargetPtr);
        target.Clear();
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EntryGetHashArgs
    {
        public void* EntryPtr;
        public uint Result;
    }

    public static int EntryGetHash(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(EntryGetHashArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for EntryGetHash: expected {Args}, got {SizeBytes}", sizeof(EntryGetHashArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<EntryGetHashArgs>();
        ref var entry = ref Unsafe.AsRef<NativeHashCollection<TKey, TValue>.Entry>(values.EntryPtr);
        values.Result = entry.Hash;
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EntryGetKeyArgs
    {
        public void* EntryPtr;
        public TKey Result;
    }

    public static int EntryGetKey(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(EntryGetKeyArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for EntryGetKey: expected {Args}, got {SizeBytes}", sizeof(EntryGetKeyArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<EntryGetKeyArgs>();
        ref var entry = ref Unsafe.AsRef<NativeHashCollection<TKey, TValue>.Entry>(values.EntryPtr);
        values.Result = entry.Key;
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EntryGetValueArgs
    {
        public void* EntryPtr;
        public TValue Result;
    }

    public static int EntryGetValue(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(EntryGetValueArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for EntryGetValue: expected {Args}, got {SizeBytes}", sizeof(EntryGetValueArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<EntryGetValueArgs>();
        ref var entry = ref Unsafe.AsRef<NativeHashCollection<TKey, TValue>.Entry>(values.EntryPtr);
        values.Result = entry.Value;
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EntryNextIsNullArgs
    {
        public void* EntryPtr;
        public byte Result;
    }

    public static int EntryNextIsNull(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(EntryNextIsNullArgs))
        {
            NativeLogging.Logger.LogError("Invalid argument size for EntryNextIsNull: expected {Args}, got {SizeBytes}", sizeof(EntryNextIsNullArgs), sizeBytes);
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<EntryNextIsNullArgs>();
        ref var entry = ref Unsafe.AsRef<NativeHashCollection<TKey, TValue>.Entry>(values.EntryPtr);
        values.Result = (byte)(entry.Next.IsNull ? 1 : 0);
        return 0;
    }
}
