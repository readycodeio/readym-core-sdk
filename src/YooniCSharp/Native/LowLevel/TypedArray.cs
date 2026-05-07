using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Yooni.Native.LowLevel;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct TypedArray<T, TStorage>
    where T : unmanaged
    where TStorage : unmanaged, IStorage<T>
{
    private TStorage _storage;
    
    public ref T this[int index]
    {
        get
        {
            var ptr = (byte*)Unsafe.AsPointer(ref _storage); 
            return ref Unsafe.AsRef<T>(ptr + index * sizeof(T));
        }
    }
    
    public int Length
        => sizeof(TStorage) / sizeof(T);
    
    [Pure]
    public TypedArrayPtr<T> GetPointer()
    {
        var ptr = Unsafe.AsPointer(ref _storage);
        return new TypedArrayPtr<T>(ptr);
    }
    
    [Pure]
    public TypedPtr<T> GetPointer(int index)
    {
        var ptr = (byte*)Unsafe.AsPointer(ref _storage);
        return new TypedPtr<T>(ptr + index * sizeof(T));
    }
}