using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Yooni.Native.LowLevel;

namespace Yooni.Native.Container.Tests;

public static unsafe class NativeStringProxy<TString>
    where TString : unmanaged, INativeString
{
    [StructLayout(LayoutKind.Sequential)]
    private struct GetLengthArgs
    {
        public void* TargetPtr;
        public int Result;
    }

    public static int GetLength(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(GetLengthArgs))
        {
            Console.WriteLine($"Invalid argument size for GetLength: expected {sizeof(GetLengthArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<GetLengthArgs>();
        ref var target = ref Unsafe.AsRef<TString>(values.TargetPtr);
        values.Result = target.Length;
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct ToBytesArgs
    {
        public void* TargetPtr;
        public int Length;
        public fixed byte Bytes[255];
    }

    public static int CopyTo(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(ToBytesArgs))
        {
            Console.WriteLine($"Invalid argument size for ToBytes: expected {sizeof(ToBytesArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<ToBytesArgs>();
        ref var target = ref Unsafe.AsRef<TString>(values.TargetPtr);

        values.Length = target.Length;

        fixed (byte* ptr = values.Bytes)
        {
            target.CopyTo(ptr);
        }

        if (values.Length < 255)
        {
            values.Bytes[values.Length] = 0;
        }

        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EqualsArgs
    {
        public void* TargetPtr;
        public TString Other;
        public byte Result;
    }

    public static int Equals(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(EqualsArgs))
        {
            Console.WriteLine($"Invalid argument size for Equals: expected {sizeof(EqualsArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<EqualsArgs>();
        ref var target = ref Unsafe.AsRef<TString>(values.TargetPtr);
        values.Result = (byte)(target.Equals(values.Other) ? 1 : 0);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GetHashCodeArgs
    {
        public void* TargetPtr;
        public int Result;
    }

    public static int GetHashCode(IntPtr args, int sizeBytes)
    {
        if (sizeBytes != sizeof(GetHashCodeArgs))
        {
            Console.WriteLine($"Invalid argument size for GetHashCode: expected {sizeof(GetHashCodeArgs)}, got {sizeBytes}");
            return 1;
        }

        var mem = new Memory(args, sizeBytes);
        ref var values = ref mem.ReadRef<GetHashCodeArgs>();
        ref var target = ref Unsafe.AsRef<TString>(values.TargetPtr);
        values.Result = target.GetHashCode();
        return 0;
    }
}