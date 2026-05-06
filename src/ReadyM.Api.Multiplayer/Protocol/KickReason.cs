namespace ReadyM.Api.Multiplayer.Protocol;

public enum KickReason : byte
{
    Unknown = 0,
    Kicked = 1,
    Banned = 2,
    ServerShutdown = 3,
}