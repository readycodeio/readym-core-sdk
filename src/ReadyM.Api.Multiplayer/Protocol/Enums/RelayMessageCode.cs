namespace ReadyM.Api.Multiplayer.Protocol.Enums;

internal enum RelayMessageCode : byte
{
    HandshakeConnected = 255,
    RequestAreaEvent = 254,
    AreaEvent = 253,
    OtherPlayerConnectionEvent = 252,
    OtherPlayerAreaEvent = 251,
    
    EcsDelta = 250,
    MaxBuiltInEvent = EcsDelta,
    
    EcsSnapshot = 249,
    EcsCreateEntity = 248,
    EcsDeleteEntity = 247,
    EcsChangeOwnership = 246,
    
    MinBuiltInEvent = EcsChangeOwnership,
    MaxAnyCustomEvent = MinBuiltInEvent - 1,
    
    MaxServerRpcEvent = MaxAnyCustomEvent,
    MinServerRpcEvent = 150,
    
    MaxClientRpcEvent = MinServerRpcEvent - 1,
    MinClientRpcEvent = 0,
    
    MinAnyCustomEvent = MinClientRpcEvent,
}
