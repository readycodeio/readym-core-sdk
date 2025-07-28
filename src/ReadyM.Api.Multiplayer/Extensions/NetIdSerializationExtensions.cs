using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.Extensions;

public static class NetIdSerializationExtensions
{
    public static bool TryGetNetworkId(this NetDataReader reader, out NetworkIdComponent result)
    {
        if (reader.AvailableBytes >= 6)
        {
            result = reader.Get<NetworkIdComponent>();
            return true;
        }

        result = default;
        return false;
    }
    
    public static bool TryGetNetworkedComponentId(this NetDataReader reader, out NetworkedComponentId result)
    {
        if (reader.AvailableBytes >= 1)
        {
            result = reader.Get<NetworkedComponentId>();
            return true;
        }

        result = default;
        return false;
    }
}