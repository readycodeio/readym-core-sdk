using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Protocol;

namespace ReadyM.Relay.Client.Shim;

public class ShimRelayMessageParser(IEnumerable<IShimRelayMessageParserImpl> impls)
{
    private readonly List<IShimRelayMessageParserImpl> _impls = [..impls];

    public object? GetBuiltInRequestCustomData(ServerEventHeader header, NetDataReader reader)
    {
        foreach (var impl in _impls)
        {
            if (!impl.SupportsRequest(header))
                continue;
            
            return impl.GetBuiltInRequestCustomDataUntyped(header, reader);
        }

        return null;
    }

    public object? GetServerRpcRequestCustomData(ServerEventHeader header, NetDataReader reader)
    {
        foreach (var impl in _impls)
        {
            if (!impl.SupportsRequest(header))
                continue;
            
            return impl.GetServerRpcRequestCustomDataUntyped(header, reader);
        }
        
        return null;
    }

    public object? GetClientRpcRequestCustomData(CustomRelayEventHeader header, NetDataReader reader)
    {
        foreach (var impl in _impls)
        {
            if (!impl.SupportsRequest(header))
                continue;
            
            return impl.GetClientRpcRequestCustomDataUntyped(header, reader);
        }
        
        return null;
    }

    public object? GetBuiltInResponseCustomData(ServerEventHeader header, NetDataReader reader)
    {
        foreach (var impl in _impls)
        {
            if (!impl.SupportsResponse(header))
                continue;
            
            return impl.GetBuiltInResponseCustomDataUntyped(header, reader);
        }
        
        return null;
    }

    public object? GetServerRpcResponseCustomData(ServerEventHeader header, NetDataReader reader)
    {
        foreach (var impl in _impls)
        {
            if (!impl.SupportsResponse(header))
                continue;
            
            return impl.GetServerRpcResponseCustomDataUntyped(header, reader);
        }
        
        return null;
    }

    public object? GetClientRpcResponseCustomData(CustomRelayEventHeader header, NetDataReader reader)
    {
        foreach (var impl in _impls)
        {
            if (!impl.SupportsResponse(header))
                continue;
            
            return impl.GetClientRpcResponseCustomDataUntyped(header, reader);
        }
        
        return null;
    }
}
