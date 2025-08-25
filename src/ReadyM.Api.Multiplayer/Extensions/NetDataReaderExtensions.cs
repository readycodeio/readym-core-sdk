using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.Extensions;

public static class NetDataReaderExtensions
{
    public static bool TryGetMetadataComponent(this NetDataReader reader, out MetadataComponent result)
    {
        if (reader.AvailableBytes >= 9)
        {
            result = reader.Get<MetadataComponent>();
            return true;
        }

        result = default;
        return false;
    }
}