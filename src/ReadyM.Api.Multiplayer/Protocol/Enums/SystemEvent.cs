namespace ReadyM.Api.Multiplayer.Protocol.Enums;

public enum SystemEvent : byte
{
    HandshakePeerIdAssigned = 255,
    HandshakeSetInitialProperties = 254,
    PlayerJoined = 253,
    PlayerLeft = 252,
    RoomStateChanged = 251,
    PlayerStateChanged = 250,
    EcsUpdate = 249,
    DestroyEntity = 248,
}