namespace Yooni.Native.LowLevel;

public enum AllocatorKind : byte
{
    Invalid = 0,
    Marshal = 1,
    Cpp = 2,
    NativeUnity = 3,
    InternalCall = 4,
#if UNITY_EDITOR || UNITY_STANDALONE
    Default = NativeUnity,
#else
    Default = Cpp,
#endif
}
