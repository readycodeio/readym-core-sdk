using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Api.Multiplayer.Protocol;

public static class Constants
{
    public static PlayerId UnsetPeerId = PlayerId.Invalid;
    public const string RoomPropertyAnnotationPrefix = "roomProperty/";
    public const string VirtualServerId = "serverId";
    public const int ClientTickRateMs = 33;
    public const int ShimClientTickRateMs = 2;
}