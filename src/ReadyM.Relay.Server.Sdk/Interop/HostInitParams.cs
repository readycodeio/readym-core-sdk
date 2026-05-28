using Yooni.Native.Container;

namespace ReadyM.Relay.Server.Sdk.Interop;

public struct HostInitParams
{
    public EcsApiPointers EcsApiPointers;
    public RpcApiPointers RpcApiPointers;
    public NativeString256 ModDirectory;
}