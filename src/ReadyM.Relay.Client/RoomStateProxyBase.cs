using System.Collections.Generic;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Relay.Client;

public class RoomStateProxyBase(RelayClient relayClient)
{
    public short MasterClientId
    {
        get => relayClient.RoomState.TryGetValue(RoomProperties.MasterClientId, out var x) ? (short)x : Constants.UnsetPeerId;
        set => SetProperty(RoomProperties.MasterClientId, value);
    }

    public int MaxPlayers
    {
        get => GetProperty<int>(RoomProperties.MaxPlayers);
        set => SetProperty(RoomProperties.MaxPlayers, value);
    }

    protected T? GetProperty<T>(object key)
    {
        if (relayClient.RoomState.TryGetValue(key, out var obj))
            return (T)obj;
        return default;
    }

    protected void SetProperty(object key, object value)
    {
        relayClient.OpSetCustomPropertiesOfRoom(new Dictionary<object, object?>
        {
            [key] = value
        });
    }
}