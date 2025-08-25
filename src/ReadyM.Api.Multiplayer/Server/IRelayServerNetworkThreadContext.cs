using System;
using LiteNetLib;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Api.Multiplayer.Server;

public interface IRelayServerNetworkThreadContext
{
    ReadOnlyDictionary<PlayerId, NetPeer> PeerByPlayer { get; }
    ReadOnlyList<PlayerId> AllPlayers { get; }
    ReadOnlyList<AreaId> Areas { get; }
    ReadOnlyList<PlayerId> GetAreaPlayers(AreaId areaId);
    ReadOnlyDictionary<PlayerId, Guid> UserGuids { get; }
}
