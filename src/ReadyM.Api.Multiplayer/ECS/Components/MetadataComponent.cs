using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MetadataComponent(NetworkId netId, ArchetypeId archetype, PlayerId owner) : IIndexedComponent<NetworkId>
{
    public readonly NetworkId NetId = netId;
    public readonly ArchetypeId Archetype = archetype;
    public PlayerId Owner = owner;

    public NetworkId GetIndexedValue() => NetId;

    // NOTE: Due to NetId field being indexed we do not want to change components in place. That eliminates the option
    // of implementing INetSerializable and INetworkedComponent
    public static void Serialize(in MetadataComponent meta, NetDataWriter writer)
    {
        writer.Put(meta.NetId);
        writer.Put(meta.Archetype);
        writer.Put(meta.Owner);
    }

    public static MetadataComponent Deserialize(NetDataReader reader)
    {
        var netId = reader.Get<NetworkId>();
        var archetype = reader.Get<ArchetypeId>();
        var owner = reader.Get<PlayerId>();
        return new MetadataComponent(netId, archetype, owner);
    }
}