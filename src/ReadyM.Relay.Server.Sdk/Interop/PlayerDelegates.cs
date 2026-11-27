using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Interop;

namespace ReadyM.Relay.Server.Sdk.Interop;

internal delegate void KickPlayerDelegate(PlayerId playerId);

internal delegate Guid GetReadyMIdDelegate(PlayerId playerId);

internal delegate void RotateCellMastersDelegate(PlayerId requester);

internal delegate void DropConnectionDelegate(PlayerId playerId);

/// <summary>1 and the area the player stands in; 0 while they are in none, or not connected.</summary>
internal unsafe delegate byte GetCurrentAreaDelegate(PlayerId playerId, FullAreaId* area);

/// <summary>
/// Fills <paramref name="buffer"/> with the players in a scope and returns how many there are. A count larger
/// than <paramref name="capacity"/> means nothing was written and the caller should come back with more room. The
/// filter's dimensions are seen; with no dimension named, that scope's players in every dimension.
/// </summary>
internal unsafe delegate int GetAreaPlayersDelegate(AreaId area, DimensionFilter filter, PlayerId* buffer, int capacity);

/// <inheritdoc cref="GetAreaPlayersDelegate"/>
internal unsafe delegate int GetCellPlayersDelegate(FullCellId cell, DimensionFilter filter, PlayerId* buffer, int capacity);
