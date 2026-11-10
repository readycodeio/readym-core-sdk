using System;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Protocol;

internal static class Constants
{
    public static PlayerId UnsetPeerId = PlayerId.Invalid;
    public const string RoomPropertyAnnotationPrefix = "roomProperty/";
    public const string AssignedPlayerList = "assignedPlayers";
    public const string VirtualServerId = "serverId";
    public const string AgonesLastAllocated = "agones.dev/last-allocated";

    /// <summary>The server loop's period: 120 Hz, as fast as the game clients run at most and send their updates.</summary>
    public static readonly TimeSpan ServerNetworkTickPeriod = TimeSpan.FromSeconds(1.0 / 120);

    public const int ClientNetworkTickRateMs = 1;
    public const int ServerEcsUpdateRateMs = 2;
    public const int ClientEcsUpdateRateMs = 2;
    public const int ClientConnectionTimeoutMs = 5000;
}