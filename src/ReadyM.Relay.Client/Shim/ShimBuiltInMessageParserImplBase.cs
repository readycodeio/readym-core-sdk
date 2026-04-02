using System;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Protocol;

namespace ReadyM.Relay.Client.Shim;

internal abstract class ShimBuiltInMessageParserImplBase<TCustomData> : ShimRelayMessageParserImplBase<TCustomData>
{
    public override bool SupportsRequest(CustomRelayEventHeader header)
        => false;
    
    public override bool SupportsResponse(CustomRelayEventHeader header)
        => false;

    public override TCustomData GetServerRpcRequestCustomData(ServerEventHeader header, NetDataReader reader)
        => throw new NotSupportedException();

    public override TCustomData GetClientRpcRequestCustomData(CustomRelayEventHeader header, NetDataReader reader)
        => throw new NotSupportedException();
    
    public override TCustomData GetServerRpcResponseCustomData(ServerEventHeader header, NetDataReader reader)
        => throw new NotSupportedException();

    public override TCustomData GetClientRpcResponseCustomData(CustomRelayEventHeader header, NetDataReader reader)
        => throw new NotSupportedException();
}