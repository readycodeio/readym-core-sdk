using System.Collections.Generic;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Relay.Client;

public class Player
{
    public Dictionary<object, object> Properties { get; set; }

    public Player(Dictionary<object, object> properties)
    {
        Properties = properties;
    }

    public int ActorNumber
    {
        get => Properties.TryGetValue(PlayerProperties.ActorNumber, out var value) ? (int)value : Constants.UnsetPlayerId;
        set => Properties[PlayerProperties.ActorNumber] = value;
    }

    public string Nickname
    {
        get => Properties.TryGetValue(PlayerProperties.NickName, out var value) ? value.ToString() : string.Empty;
        set => Properties[PlayerProperties.NickName] = value;
    }
}