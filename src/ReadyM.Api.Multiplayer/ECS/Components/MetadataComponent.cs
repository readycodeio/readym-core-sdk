using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MetadataComponent(NetworkIdComponent netId, ArchetypeId archetype, PlayerId owner) : IIndexedComponent<NetworkIdComponent>, INetSerializable
{
    public NetworkIdComponent NetId = netId;
    public ArchetypeId Archetype = archetype;
    public PlayerId Owner = owner;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(Archetype);
        writer.Put(Owner);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.Get<NetworkIdComponent>();
        Archetype = reader.Get<ArchetypeId>();
        Owner = reader.Get<PlayerId>();
    }

    public NetworkIdComponent GetIndexedValue() => NetId;
}