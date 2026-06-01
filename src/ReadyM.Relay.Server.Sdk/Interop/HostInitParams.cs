using Yooni.Native.Container;

namespace ReadyM.Relay.Server.Sdk.Interop;

public struct HostInitParams
{
    public required EcsApiPointers EcsApiPointers;
    public required RpcApiPointers RpcApiPointers;
    public required NativeString256 ModDirectory;
}