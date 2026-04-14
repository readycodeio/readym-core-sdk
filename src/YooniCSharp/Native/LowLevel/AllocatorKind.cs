namespace Yooni.Native.LowLevel;

public enum AllocatorKind : byte
{
    InternalCall = 0,
    Marshal = 1,
    NativeUnity = 2,
    Cpp = 3,
#if UNITY_EDITOR || UNITY_STANDALONE
    Default = NativeUnity,
#elif UNREAL_ENGINE
    Default = Delegated,
#else
    Default = Cpp,
#endif
}