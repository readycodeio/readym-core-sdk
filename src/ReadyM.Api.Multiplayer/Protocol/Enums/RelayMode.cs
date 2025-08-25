namespace ReadyM.Api.Multiplayer.Protocol.Enums;

public enum RelayMode : byte
{
    AreaOfInterestOthers = 0,
    AreaOfInterestAll = 1,
    GlobalOthers = 2,
    GlobalAll = 3,
    EntityOwner = 4,
    Peers = 5,
}