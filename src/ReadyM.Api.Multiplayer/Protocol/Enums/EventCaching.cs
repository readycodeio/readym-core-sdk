namespace ReadyM.Api.Multiplayer.Protocol.Enums;

public enum EventCaching : byte
{
    DoNotCache = 0,
    AddToRoomCache = 1,
    AddToRoomCacheGlobal = 2,
    RemoveFromRoomCache = 3
}