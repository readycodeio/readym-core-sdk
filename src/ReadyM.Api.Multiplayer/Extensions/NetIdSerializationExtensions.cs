using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace ReadyM.Api.Multiplayer.Extensions;

internal static class NetIdSerializationExtensions
{
    public static bool TryGetNetworkId(this NetDataReader reader, out NetworkId result)
    {
        // A VarUShort creator and a VarUInt id, a byte each at the least
        if (reader.AvailableBytes >= 2)
        {
            result = reader.Get<NetworkId>();
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