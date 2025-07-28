using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Relay.Common.Protocol;

public static class Constants
{
    public static PlayerId UnsetPeerId = PlayerId.Invalid;
    public const string RoomPropertyAnnotationPrefix = "roomProperty/";
    public const string VirtualServerId = "serverId";
    public const int ServerNetworkTickRateMs = 1;
    public const int ClientNetworkTickRateMs = 2;
    public const int ShimClientTickRateMs = 1;
    public const int ServerEcsUpdateRateMs = 15;
    public const int ClientEcsUpdateRateMs = 33;
}