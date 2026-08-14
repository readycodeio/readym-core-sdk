namespace ReadyM.Relay.Server.Sdk.Interop;

public struct HostInitParams
{
    public required EcsApiPointers EcsApiPointers;
    public required RpcApiPointers RpcApiPointers;
    public required ArchetypePointers ArchetypePointers;
    public required PlayerApiPointers PlayerApiPointers;
    public required ServerEventsPointers ServerEventsPointers;
}