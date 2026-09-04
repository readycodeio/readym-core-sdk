using ReadyM.Api.Idents;

namespace ReadyM.Relay.Server.Sdk.Interop;

internal delegate void PlayerEventHandlerDelegate(PlayerEventData data);

internal delegate void AddPlayerEventHandlerDelegate(PlayerEventHandlerDelegate handler);

internal delegate void RemovePlayerEventHandlerDelegate(PlayerEventHandlerDelegate handler);

internal delegate void KickPlayerDelegate(PlayerId playerId);

internal delegate Guid GetReadyMIdDelegate(PlayerId playerId);

internal delegate void RotateCellMastersDelegate(PlayerId requester);
