namespace ReadyM.Api.Multiplayer.Protocol.Enums;

public enum RelayMessageCode : byte
{
    HandshakeConnected = 255,
    RequestAreaEvent = 254,
    AreaEvent = 253,
    OtherPlayerConnectionEvent = 252,
    OtherPlayerAreaEvent = 251,
    
    EcsUpdate = 250,
    EcsSnapshot = 249,
    EcsCreateEntity = 248,
    EcsDeleteEntity = 247,
    
    RequestDownloadBlob = 246,
    DownloadBlobData = 245,
    RequestUploadBlob = 244,
    UploadBlobAck = 243,

    MaxServerRpcEvent = UploadBlobAck - 1,
    MinServerRpcEvent = 150,
    
    MaxCustomEvent = MinServerRpcEvent - 1,
    MinCustomEvent = 0,
}
