using ReadyM.Api.Idents;

namespace ReadyM.Relay.Server.Sdk.Interop;

public unsafe delegate void PlayerEventHandlerDelegate(byte* data, int size);

public delegate void AddPlayerEventHandlerDelegate(PlayerEventHandlerDelegate handler);

public delegate void RemovePlayerEventHandlerDelegate(PlayerEventHandlerDelegate handler);

public delegate void KickPlayerDelegate(PlayerId playerId);

public unsafe delegate byte GetReadyMIdDelegate(PlayerId playerId, Guid* readyMId);
