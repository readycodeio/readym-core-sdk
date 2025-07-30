namespace ReadyM.Api.Multiplayer.Protocol.Enums;

public enum RelayMessageCode : byte
{
    HandshakeConnected = 255,
    RequestAreaEvent = 254,
    AreaEvent = 253,
    OtherPlayerConnectionEvent = 252,
    OtherPlayerAreaEvent = 251,
    
    MaxBuiltInEvent = EcsUpdate,

    EcsUpdate = 250,
    EcsSnapshot = 249,
    EcsCreateEntity = 248,
    EcsDeleteEntity = 247,
    
    RequestDownloadBlob = 246,
    DownloadBlobData = 245,
    RequestUploadBlob = 244,
    UploadBlobAck = 243,
    
    MinBuiltInEvent = UploadBlobAck,
    MaxAnyCustomEvent = MinBuiltInEvent - 1,
    
    MaxServerRpcEvent = MaxAnyCustomEvent,
    MinServerRpcEvent = 150,
    
    MaxClientRpcEvent = MinServerRpcEvent - 1,
    MinClientRpcEvent = 0,
    
    MinAnyCustomEvent = MinClientRpcEvent,
}
