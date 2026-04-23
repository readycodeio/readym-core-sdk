namespace Yooni.Native.LowLevel;

public static unsafe class Allocator
{
    public static void* Alloc(int size, AllocatorKind kind)
    {
        switch (kind)
        {
            case AllocatorKind.InternalCall:
                return InternalCallAllocatorImpl.AllocMemory(size);
            case AllocatorKind.NativeUnity:
                return NativeUnityAllocatorImpl.AllocMemory(size);
            case AllocatorKind.Marshal:
                return MarshalAllocatorImpl.AllocMemory(size);
            case AllocatorKind.Cpp:
                return CppAllocatorImpl.AllocMemory(size);
            default:
                return null;
        }
    }

    public static void Free(void* ptr, AllocatorKind kind)
    {
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
                break;
        }
    }
}