using System.Collections.Generic;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Relay.Client;

public class RoomStateProxyBase(IRelayClient relayClient)
{
    public IRelayClient RelayClient
        => relayClient;
    
    public PlayerId MasterClientId
    {
        get => RelayClient.RoomState.TryGetValue(RoomProperties.MasterClientId, out var x) ? (PlayerId)x : Constants.UnsetPeerId;
        set => SetProperty(RoomProperties.MasterClientId, value);
    }

    public int MaxPlayers
    {
        get => GetProperty<int>(RoomProperties.MaxPlayers);
        set => SetProperty(RoomProperties.MaxPlayers, value);
    }

    protected T? GetProperty<T>(object key)
    {
        if (RelayClient.RoomState.TryGetValue(key, out var obj))
            return (T)obj;
        return default;
    }

    protected void SetProperty(object key, object value)
    {
        RelayClient.OpSetCustomPropertiesOfRoom(new Dictionary<object, object?>
        {
            [key] = value
        });
    }
}
