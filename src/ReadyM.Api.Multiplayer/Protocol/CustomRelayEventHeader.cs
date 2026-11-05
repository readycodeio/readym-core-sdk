using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.Protocol;

/// <exclude />
/// <summary>
/// Spans 4 bytes in standard relay modes and (6 + 2 * peers) in peer-targeted relay mode.
/// - 1 byte for eventCode
/// - 2 bytes for sender
/// - 2 bytes for peers count
/// - 1 byte for relayMode
/// - peers * 2 bytes for peers
/// </summary>
[DeriveJsonSerializable(SerializableMode.Default | SerializableMode.MapInternal)]
public partial struct CustomRelayEventHeader
{
    public RelayMessageCode EventCode;
    public PlayerId Sender;
    public PlayerId[]? Peers;
    internal RelayMode RelayMode;

    internal CustomRelayEventHeader(
        RelayMessageCode eventCode,
        PlayerId sender,
        PlayerId[]? peers,
        RelayMode relayMode = RelayMode.AreaOfInterestOthers)
    {
        EventCode = eventCode;
        Sender = sender;
        Peers = peers;
        RelayMode = relayMode;
    }
}