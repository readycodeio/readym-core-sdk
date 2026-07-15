using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.Protocol;

/// <summary>
/// Spans 3 bytes.
/// - 1 byte for eventCode
/// - 2 bytes for sender
/// </summary>
[DeriveJsonSerializable(mode: SerializableMode.MapFields | SerializableMode.MapPublic)]
internal partial struct ServerEventHeader(RelayMessageCode eventCode, PlayerId sender)
{
    public RelayMessageCode EventCode = eventCode;
    public PlayerId Sender = sender;
}
