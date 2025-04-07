using System.Collections.Generic;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Relay.Client;

public class Room
{
    public Dictionary<object, object> Properties { get; } = new();

    public string RoomId
    {
        get => Properties.TryGetValue(RoomProperties.RoomId, out var value) ? value.ToString() : string.Empty;
        set => Properties[RoomProperties.RoomId] = value;
    }

    public int MasterClientId => 
        Properties.TryGetValue(RoomProperties.MasterClientId, out var value) ? (int)value : Constants.UnsetPlayerId;

    public bool IsOpen
    {
        get => Properties.TryGetValue(RoomProperties.IsOpen, out var value) && (bool)value;
        set => Properties[RoomProperties.IsOpen] = value;
    }

    public bool IsVisible
    {
        get => Properties.TryGetValue(RoomProperties.IsVisible, out var value) && (bool)value;
        set => Properties[RoomProperties.IsVisible] = value;
    }

    public int MaxPlayers
    {
        get => Properties.TryGetValue(RoomProperties.MaxPlayers, out var value) ? (int)value : 0;
        set => Properties[RoomProperties.MaxPlayers] = value;
    }
}