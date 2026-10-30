namespace ReadyM.Api.Multiplayer.Protocol.Enums;

/// <exclude />
/// <summary>
/// A unique identifier of a single message type. Mods' RPCs and networked events share the codes from
/// <see cref="MinAnyCustomEvent"/> up. Sent as a <c>VarUShort</c>.
/// </summary>
public enum RelayMessageCode : ushort
{
    HandshakeConnected = 0,
    RequestAreaEvent = 1,
    AreaEvent = 2,
    OtherPlayerConnectionEvent = 3,
    OtherPlayerAreaEvent = 4,
    EcsDelta = 5,

    MinBuiltInEvent = EcsDelta,

    EcsSnapshot = 6,
    EcsCreateEntity = 7,
    EcsDeleteEntity = 8,
    EcsChangeOwnership = 9,
    RequestSetActiveCellsEvent = 10,
    EcsChangeScope = 11,
    EcsInternedStrings = 12,

    MaxBuiltInEvent = EcsInternedStrings,
    MinAnyCustomEvent = MaxBuiltInEvent + 1,
}
