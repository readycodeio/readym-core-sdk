using System.Collections.Generic;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Relay.Client;

public class Room(RelayClient client)
{
    public Dictionary<object, object> Properties { get; } = new();

    public int MasterClientId
    {
        get => Properties.TryGetValue(RoomProperties.MasterClientId, out var value) ? (int)value : Constants.UnsetPlayerId;
        set
        {
            Properties[RoomProperties.MasterClientId] = value;
            client.OpSetCustomPropertiesOfRoom(new() { [RoomProperties.MasterClientId] = value });
        }
    }

    public int MaxPlayers
    {
        get => Properties.TryGetValue(RoomProperties.MaxPlayers, out var value) ? (int)value : 0;
        set
        {
            Properties[RoomProperties.MaxPlayers] = value;
            client.OpSetCustomPropertiesOfRoom(new() { [RoomProperties.MaxPlayers] = value });
        }
    }
}