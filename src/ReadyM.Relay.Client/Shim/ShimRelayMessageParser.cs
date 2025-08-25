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
        var position = reader.Position;
        foreach (var impl in _impls)
        {
            if (!impl.SupportsRequest(header))
                continue;
            
            var result = impl.GetBuiltInRequestCustomDataUntyped(header, reader);
            reader.SetPosition(position);
            return result;
        }

        return null;
    }

    public object? GetServerRpcRequestCustomData(ServerEventHeader header, NetDataReader reader)
    {
        var position = reader.Position;
        foreach (var impl in _impls)
        {
            if (!impl.SupportsRequest(header))
                continue;
            
            var result = impl.GetServerRpcRequestCustomDataUntyped(header, reader);
            reader.SetPosition(position);
            return result;
        }
        
        return null;
    }

    public object? GetClientRpcRequestCustomData(CustomRelayEventHeader header, NetDataReader reader)
    {
        var position = reader.Position;
        foreach (var impl in _impls)
        {
            if (!impl.SupportsRequest(header))
                continue;
            
            var result = impl.GetClientRpcRequestCustomDataUntyped(header, reader);
            reader.SetPosition(position);
            return result;
        }
        
        return null;
    }

    public object? GetBuiltInResponseCustomData(ServerEventHeader header, NetDataReader reader)
    {
        var position = reader.Position;
        foreach (var impl in _impls)
        {
            if (!impl.SupportsResponse(header))
                continue;

            var result = impl.GetBuiltInResponseCustomDataUntyped(header, reader);
            reader.SetPosition(position);
            return result;
        }
        
        return null;
    }

    public object? GetServerRpcResponseCustomData(ServerEventHeader header, NetDataReader reader)
    {
        var position = reader.Position;
        foreach (var impl in _impls)
        {
            if (!impl.SupportsResponse(header))
                continue;
            
            var result = impl.GetServerRpcResponseCustomDataUntyped(header, reader);
            reader.SetPosition(position);
            return result;
        }
        
        return null;
    }

    public object? GetClientRpcResponseCustomData(CustomRelayEventHeader header, NetDataReader reader)
    {
        var position = reader.Position;
        foreach (var impl in _impls)
        {
            if (!impl.SupportsResponse(header))
                continue;
            
            var result = impl.GetClientRpcResponseCustomDataUntyped(header, reader);
            reader.SetPosition(position);
            return result;
        }
        
        return null;
    }
}
