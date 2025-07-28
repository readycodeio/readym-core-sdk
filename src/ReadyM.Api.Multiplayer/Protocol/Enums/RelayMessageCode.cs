namespace ReadyM.Api.Multiplayer.Protocol.Enums;

public enum RelayMessageCode : byte
{
    HandshakePeerIdAssigned = 255,
    HandshakeSetInitialProperties = 254,
    PlayerJoined = 253,
    PlayerLeft = 252,
    RoomStateChanged = 251,
    PlayerStateChanged = 250,
    EcsUpdate = 249,
    EcsSnapshot = 248,
    DestroyEntity = 247,
    DownloadBlob = 246,
    BlobData = 245,
    UploadBlob = 244,
    UploadBlobAck = 243,
    MaxCustomEvent = UploadBlobAck - 1,
    MinCustomEvent = 0,
}
