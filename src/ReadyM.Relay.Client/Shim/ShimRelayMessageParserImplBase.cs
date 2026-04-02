using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Protocol;

namespace ReadyM.Relay.Client.Shim;

internal abstract class ShimRelayMessageParserImplBase<TCustomData> : IShimRelayMessageParserImpl<TCustomData>
{
    public abstract bool SupportsRequest(ServerEventHeader header);
    public abstract bool SupportsRequest(CustomRelayEventHeader header);
    public abstract bool SupportsResponse(ServerEventHeader header);
    public abstract bool SupportsResponse(CustomRelayEventHeader header);

    public object? GetBuiltInRequestCustomDataUntyped(ServerEventHeader header, NetDataReader reader)
        => GetBuiltInRequestCustomData(header, reader);

    public object? GetServerRpcRequestCustomDataUntyped(ServerEventHeader header, NetDataReader reader)
        => GetServerRpcRequestCustomData(header, reader);

    public object? GetClientRpcRequestCustomDataUntyped(CustomRelayEventHeader header, NetDataReader reader)
        => GetClientRpcRequestCustomData(header, reader);

    public object? GetBuiltInResponseCustomDataUntyped(ServerEventHeader header, NetDataReader reader)
        => GetBuiltInResponseCustomData(header, reader);

    public object? GetServerRpcResponseCustomDataUntyped(ServerEventHeader header, NetDataReader reader)
        => GetServerRpcResponseCustomData(header, reader);

    public object? GetClientRpcResponseCustomDataUntyped(CustomRelayEventHeader header, NetDataReader reader)
        => GetClientRpcResponseCustomData(header, reader);

    public abstract TCustomData GetBuiltInRequestCustomData(ServerEventHeader header, NetDataReader reader);
    public abstract TCustomData GetServerRpcRequestCustomData(ServerEventHeader header, NetDataReader reader);
    public abstract TCustomData GetClientRpcRequestCustomData(CustomRelayEventHeader header, NetDataReader reader);
    public abstract TCustomData GetBuiltInResponseCustomData(ServerEventHeader header, NetDataReader reader);
    public abstract TCustomData GetServerRpcResponseCustomData(ServerEventHeader header, NetDataReader reader);
    public abstract TCustomData GetClientRpcResponseCustomData(CustomRelayEventHeader header, NetDataReader reader);
}