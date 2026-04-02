using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Protocol;

namespace ReadyM.Relay.Client.Shim;

internal interface IShimRelayMessageParserImpl
{
    bool SupportsRequest(ServerEventHeader header);
    bool SupportsRequest(CustomRelayEventHeader header);
    bool SupportsResponse(ServerEventHeader header);
    bool SupportsResponse(CustomRelayEventHeader header);
    
    object? GetBuiltInRequestCustomDataUntyped(ServerEventHeader header, NetDataReader reader);
    object? GetServerRpcRequestCustomDataUntyped(ServerEventHeader header, NetDataReader reader);
    object? GetClientRpcRequestCustomDataUntyped(CustomRelayEventHeader header, NetDataReader reader);
    object? GetBuiltInResponseCustomDataUntyped(ServerEventHeader header, NetDataReader reader);
    object? GetServerRpcResponseCustomDataUntyped(ServerEventHeader header, NetDataReader reader);
    object? GetClientRpcResponseCustomDataUntyped(CustomRelayEventHeader header, NetDataReader reader);
}

internal interface IShimRelayMessageParserImpl<out TCustomData> : IShimRelayMessageParserImpl
{
    TCustomData GetBuiltInRequestCustomData(ServerEventHeader header, NetDataReader reader);
    TCustomData GetServerRpcRequestCustomData(ServerEventHeader header, NetDataReader reader);
    TCustomData GetClientRpcRequestCustomData(CustomRelayEventHeader header, NetDataReader reader);
    TCustomData GetBuiltInResponseCustomData(ServerEventHeader header, NetDataReader reader);
    TCustomData GetServerRpcResponseCustomData(ServerEventHeader header, NetDataReader reader);
    TCustomData GetClientRpcResponseCustomData(CustomRelayEventHeader header, NetDataReader reader);
}