using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[NativeComponent]
[StructLayout(LayoutKind.Sequential)]
public struct MetadataComponent(NetworkId netId, ArchetypeId archetype, PlayerId owner) : IIndexedComponent<NetworkId>, INetSerializable
{
    public NetworkId NetId { get; private set; } = netId;
    public ArchetypeId Archetype { get; private set; } = archetype;
    public PlayerId Owner = owner;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(Archetype);
        writer.Put(Owner);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.Get<NetworkId>();
        Archetype = reader.Get<ArchetypeId>();
        Owner = reader.Get<PlayerId>();
    }

    public NetworkId GetIndexedValue() => NetId;
}