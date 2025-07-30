using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.Protocol;

/// <summary>
/// Spans 3 bytes.
/// - 1 byte for eventCode
/// - 2 bytes for sender
/// </summary>
[DeriveJsonSerializable]
public readonly partial struct ServerEventHeader(byte eventCode, PlayerId sender)
{
    public readonly byte EventCode = eventCode;
    public readonly PlayerId Sender = sender;
}
