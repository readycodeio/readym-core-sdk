namespace ReadyM.Api.Multiplayer.Protocol;

public enum DisconnectedReason : byte
{
    Unknown = 0,
    ClientDisconnected,
    IncompatibleVersion,
    ExpiredTicket,
    AlreadyConnected,
    ServerFull,
    Kicked,
    Banned,
}