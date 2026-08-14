using LiteNetLib;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Interop;

public enum ServerEventKind : byte
{
    PlayerConnected = 0,
    PlayerDisconnected = 1,
    AreaCreated = 2,
    AreaDeleted = 3,
    PlayerJoinedArea = 4,
    PlayerLeftArea = 5,
    CellCreated = 6,
    CellDeleted = 7,
    PlayerActivatedCell = 8,
    PlayerDeactivatedCell = 9,
}

public delegate void ServerEventHandlerDelegate(ServerEventKind kind, ServerEventPayload payload);

public delegate void SubscribeServerEventsDelegate(ServerEventHandlerDelegate handler);

public delegate void UnsubscribeServerEventsDelegate(ServerEventHandlerDelegate handler);
