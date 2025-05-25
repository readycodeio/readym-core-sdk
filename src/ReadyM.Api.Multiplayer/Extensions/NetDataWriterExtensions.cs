using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.Extensions;

public static class NetDataWriterExtensions
{
    public static void Put(this NetDataWriter writer, NetworkIdComponent id)
    {
        writer.Put(id.Owner);
        writer.Put(id.Id);
    }

    public static NetworkIdComponent GetNetworkId(this NetDataReader reader)
    {
        var owner = reader.GetShort();
        var id = reader.GetUInt();
        return new NetworkIdComponent(owner, id);
    }

    public static bool TryGetNetworkId(this NetDataReader reader, out NetworkIdComponent result)
    {
        if (reader.AvailableBytes >= 6)
        {
            result = reader.GetNetworkId();
            return true;
        }

        result = default;
        return false;
    }
}