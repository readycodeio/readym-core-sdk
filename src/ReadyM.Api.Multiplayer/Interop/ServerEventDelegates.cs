namespace ReadyM.Api.Multiplayer.Interop;

internal enum ServerEventKind : byte
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
    WorldEntityCreated = 10,
    BeforeWorldSave = 11,
    AfterWorldSave = 12,
    BeforePlayerSave = 13,
    AfterPlayerSave = 14,
}

internal delegate void ServerEventHandlerDelegate(ServerEventKind kind, ServerEventPayload payload);

internal delegate void SubscribeServerEventsDelegate(ServerEventHandlerDelegate handler);

internal delegate void UnsubscribeServerEventsDelegate(ServerEventHandlerDelegate handler);
