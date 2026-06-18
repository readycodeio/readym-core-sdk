namespace Yooni.Native.LowLevel;

public enum AllocatorKind : byte
{
    Cpp = 0,
    Marshal = 1,
    NativeUnity = 2,
    InternalCall = 3,
#if UNITY_EDITOR || UNITY_STANDALONE
    Default = NativeUnity,
#else
    Default = Cpp,
#endif
}