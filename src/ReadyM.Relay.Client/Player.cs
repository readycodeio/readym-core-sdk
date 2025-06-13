using System.Collections.Generic;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;

namespace ReadyM.Relay.Client;

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