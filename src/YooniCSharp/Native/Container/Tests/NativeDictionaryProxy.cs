using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container.Tests;

public static unsafe class NativeDictionaryProxy<TKey, TValue, THash>
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
    where THash : struct, IHashFunction<TKey>
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
        ref var target = ref Unsafe.AsRef<NativeDictionary<TKey, TValue, THash>>(values.TargetPtr);
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
        ref var target = ref Unsafe.AsRef<NativeDictionary<TKey, TValue, THash>>(values.TargetPtr);
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
        ref var target = ref Unsafe.AsRef<NativeDictionary<TKey, TValue, THash>>(values.TargetPtr);
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
        ref var target = ref Unsafe.AsRef<NativeDictionary<TKey, TValue, THash>>(values.TargetPtr);
        values.Result = target.Capacity;
        return 0;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct GetItemArgs
    {
        public void* TargetPtr;
        public TKey Key;
        public TValue Result;
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
        ref var target = ref Unsafe.AsRef<NativeDictionary<TKey, TValue, THash>>(values.TargetPtr);
        values.Result = target[values.Key];
        return 0;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct SetItemArgs
    {
        public void* TargetPtr;
        public TKey Key;
        public TValue Value;
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
        ref var target = ref Unsafe.AsRef<NativeDictionary<TKey, TValue, THash>>(values.TargetPtr);
        target[values.Key] = values.Value;
        return 0;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct AddArgs
    {
        public void* TargetPtr;
        public TKey Key;
        public TValue Value;
        public byte Result;
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
        
        ref var target = ref Unsafe.AsRef<NativeDictionary<TKey, TValue, THash>>(values.TargetPtr);
        values.Result = (byte)(target.Add(values.Key, values.Value) ? 1 : 0);
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
        ref var target = ref Unsafe.AsRef<NativeDictionary<TKey, TValue, THash>>(values.TargetPtr);
        target.Clear();
        return 0;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct ContainsArgs
    {
        public void* TargetPtr;
        public TKey Key;
        public TValue Value;
        public byte Result;
    }
    
    public static int Contains(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(ContainsArgs))
        {
            Console.WriteLine($"Invalid argument size for Contains: expected {sizeof(ContainsArgs)}, got {sizeBytes}");
            return 1;
        }
        
        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<ContainsArgs>();
        ref var target = ref Unsafe.AsRef<NativeDictionary<TKey, TValue, THash>>(values.TargetPtr);
        values.Result = (byte)(target.Contains(values.Key, values.Value) ? 1 : 0);
        return 0;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct ContainsKeyArgs
    {
        public void* TargetPtr;
        public TKey Key;
        public byte Result;
    }
    
    public static int ContainsKey(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(ContainsKeyArgs))
        {
            Console.WriteLine($"Invalid argument size for ContainsKey: expected {sizeof(ContainsKeyArgs)}, got {sizeBytes}");
            return 1;
        }
        
        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<ContainsKeyArgs>();
        ref var target = ref Unsafe.AsRef<NativeDictionary<TKey, TValue, THash>>(values.TargetPtr);
        values.Result = (byte)(target.ContainsKey(values.Key) ? 1 : 0);
        return 0;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct RemoveArgs
    {
        public void* TargetPtr;
        public TKey Key;
        public byte Result;
    }
    
    public static int Remove(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(RemoveArgs))
        {
            Console.WriteLine($"Invalid argument size for Remove: expected {sizeof(RemoveArgs)}, got {sizeBytes}");
            return 1;
        }
        
        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<RemoveArgs>();
        ref var target = ref Unsafe.AsRef<NativeDictionary<TKey, TValue, THash>>(values.TargetPtr);
        values.Result = (byte)(target.Remove(values.Key) ? 1 : 0);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TryGetValueArgs
    {
        public void* TargetPtr;
        public TKey Key;
        public TValue* Value;
        public byte Result;
    }
    
    public static int TryGetValue(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(TryGetValueArgs))
        {
            Console.WriteLine($"Invalid argument size for TryGetValue: expected {sizeof(TryGetValueArgs)}, got {sizeBytes}");
            return 1;
        }
        
        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<TryGetValueArgs>();
        ref var target = ref Unsafe.AsRef<NativeDictionary<TKey, TValue, THash>>(values.TargetPtr);
        values.Result = (byte)(target.TryGetValue(values.Key, out *values.Value) ? 1 : 0);
        return 0;
    }
}