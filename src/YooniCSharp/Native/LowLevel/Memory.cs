using System;
using System.Runtime.CompilerServices;

namespace Yooni.Native.LowLevel;

public unsafe struct Memory
{
    private void* _ptr;
    private int _length;

    public Memory(void* ptr, int length)
    {
        _ptr = ptr;
        _length = length;
    }
    
    public Memory(IntPtr ptr, int length)
    {
        _ptr = (void*)ptr;
        _length = length;
    }

    public T Get<T>()
        where T : unmanaged
    {
        if (sizeof(T) > _length)
            throw new InvalidOperationException($"Cannot peek type {typeof(T)}: not enough data left in buffer ({_length} bytes remaining, but {sizeof(T)} bytes required)");
        
        return Unsafe.AsRef<T>(_ptr);
    }

    public T* GetPtr<T>()
        where T : unmanaged
    {
        if (sizeof(IntPtr) > _length)
            throw new InvalidOperationException($"Cannot peek pointer for type {typeof(T)}: not enough data left in buffer ({_length} bytes remaining, but {sizeof(IntPtr)} bytes required)");
        
        return (T*)Unsafe.AsRef<IntPtr>(_ptr).ToPointer();
    }

    public ref T GetRef<T>()
        where T : unmanaged
    {
        if (sizeof(T) > _length)
            throw new InvalidOperationException($"Cannot peek reference for type {typeof(T)}: not enough data left in buffer ({_length} bytes remaining, but {sizeof(T)} bytes required)");
        
        return ref Unsafe.AsRef<T>(_ptr);
    }
    
    public void Set<T>(in T value)
        where T : unmanaged
    {
        if (sizeof(T) > _length)
            throw new InvalidOperationException($"Cannot peek type {typeof(T)}: not enough data left in buffer ({_length} bytes remaining, but {sizeof(T)} bytes required)");

        Unsafe.AsRef<T>(_ptr) = value;
    }

    public void SetPtr<T>(T* value)
        where T : unmanaged
    {
        if (sizeof(IntPtr) > _length)
            throw new InvalidOperationException($"Cannot peek pointer for type {typeof(T)}: not enough data left in buffer ({_length} bytes remaining, but {sizeof(IntPtr)} bytes required)");
        
        Unsafe.AsRef<IntPtr>(_ptr) = new IntPtr(value);
    }

    public T Read<T>()
        where T : unmanaged
    {
        var result = Get<T>();
        
        _ptr = (byte*)_ptr + sizeof(T);
        _length -= sizeof(T);
        
        return result;
    }

    public T* ReadPtr<T>()
        where T : unmanaged
    {
        var ptrResult = GetPtr<T>();

        _ptr = (byte*)_ptr + sizeof(IntPtr);
        _length -= sizeof(IntPtr);
        
        return (T*)ptrResult;
    }

    public ref T ReadRef<T>()
        where T : unmanaged
    {
        ref var result = ref GetRef<T>();
        
        _ptr = (byte*)_ptr + sizeof(T);
        _length -= sizeof(T);
        
        return ref result;
    }
    
    public void Write<T>(T value)
        where T : unmanaged
    {
        Set(value);
        
        _ptr = (byte*)_ptr + sizeof(T);
        _length -= sizeof(T);
    }

    public void WritePtr<T>(T* value)
        where T : unmanaged
    {
        SetPtr(value);

        _ptr = (byte*)_ptr + sizeof(IntPtr);
        _length -= sizeof(IntPtr);
    }
}