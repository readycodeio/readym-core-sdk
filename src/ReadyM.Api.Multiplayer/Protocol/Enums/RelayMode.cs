using System;

namespace ReadyM.Api.Multiplayer.Protocol.Enums;

public enum RelayMode : byte
{
    AreaOfInterestOthers = 0,
    AreaOfInterestAll = 1,
    GlobalOthers = 2,
    GlobalAll = 3,

    [Obsolete("This needs to be fixed, as almost no event that uses it actually passes the NetId required by the server")]
    EntityOwner = 4,
    Peers = 5,
}