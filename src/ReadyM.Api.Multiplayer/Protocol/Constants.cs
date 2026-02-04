using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Api.Multiplayer.Protocol;

public static class Constants
{
    public static PlayerId UnsetPeerId = PlayerId.Invalid;
    public const string RoomPropertyAnnotationPrefix = "roomProperty/";
    public const string AssignedPlayerList = "assignedPlayers";
    public const string VirtualServerId = "serverId";
    public const string RegionLabel = "region";
    public const string AgonesLastAllocated = "agones.dev/last-allocated";
    public const int ServerNetworkTickRateMs = 5;
    public const int ClientNetworkTickRateMs = 2;
    public const int ShimClientTickRateMs = 1;
    public const int ServerEcsUpdateRateMs = 16;
    public const int ClientEcsUpdateRateMs = 16;
    public const int ClientConnectionTimeoutMs = 5000;
}