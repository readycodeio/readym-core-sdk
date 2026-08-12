using ReadyM.Api.Idents;

namespace ReadyM.Relay.Server.Sdk.Interop;

public delegate void PlayerEventHandlerDelegate(PlayerEventData data);

public delegate void AddPlayerEventHandlerDelegate(PlayerEventHandlerDelegate handler);

public delegate void RemovePlayerEventHandlerDelegate(PlayerEventHandlerDelegate handler);

public delegate void KickPlayerDelegate(PlayerId playerId);

public delegate Guid GetReadyMIdDelegate(PlayerId playerId);
