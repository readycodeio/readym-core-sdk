using System;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Multiplayer.ECS.Components;

/// <exclude />
public interface INetworkedComponent : IReadyComponent, INetSerializable
{
    void ClearDirty();
    void ClearDirty(int field);
    bool IsDirty { get; }
    void WriteDelta(NetDataWriter writer);
    void ReadDelta(NetDataReader reader);
    void ReadDeltaTracking(NetDataReader reader, int id);
    void DeserializeTracking(NetDataReader reader, int id);
    Type GetChangeComponent();
}
