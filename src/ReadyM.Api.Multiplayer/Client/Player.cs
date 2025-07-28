using System.Collections.Generic;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Client;

public class Player(Dictionary<object, object> properties)
{
    public Dictionary<object, object> Properties { get; } = properties;

    // TODO: Optimize access
    public PlayerId PlayerId
    {
        get => Properties.TryGetValue(PlayerProperties.PlayerId, out var value) ? (PlayerId)value : Constants.UnsetPeerId;
        set => Properties[PlayerProperties.PlayerId] = value;
    }
}