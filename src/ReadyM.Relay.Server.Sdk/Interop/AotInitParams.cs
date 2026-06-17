using Yooni.Native.Container;

namespace ReadyM.Relay.Server.Sdk.Interop;

public struct AotInitParams
{
    public required NativeString256 ModDirectory;
    public required AotPointers AotPointers;
}