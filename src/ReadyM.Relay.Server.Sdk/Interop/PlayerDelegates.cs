using ReadyM.Api.Idents;

namespace ReadyM.Relay.Server.Sdk.Interop;

internal delegate void KickPlayerDelegate(PlayerId playerId);

internal delegate Guid GetReadyMIdDelegate(PlayerId playerId);

internal delegate void RotateCellMastersDelegate(PlayerId requester);

internal delegate void DropConnectionDelegate(PlayerId playerId);

/// <summary>1 and the area the player stands in; 0 while they are in none, or not connected.</summary>
internal unsafe delegate byte GetCurrentAreaDelegate(PlayerId playerId, FullAreaId* area);

/// <summary>
/// Fills <paramref name="buffer"/> with the players in a scope and returns how many there are. A count larger
/// than <paramref name="capacity"/> means nothing was written and the caller should come back with more room. With
/// <paramref name="everyDimension"/> set, the scope's players in every dimension, whatever dimension the id names.
/// </summary>
internal unsafe delegate int GetAreaPlayersDelegate(FullAreaId area, byte everyDimension, PlayerId* buffer, int capacity);

/// <inheritdoc cref="GetAreaPlayersDelegate"/>
internal unsafe delegate int GetCellPlayersDelegate(FullCellId cell, byte everyDimension, PlayerId* buffer, int capacity);
