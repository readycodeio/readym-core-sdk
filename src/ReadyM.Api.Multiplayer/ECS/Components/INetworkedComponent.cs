using Friflo.Engine.ECS;
using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.ECS.Components;

/// <exclude />
public interface INetworkedComponent : IComponent, INetSerializable
{
    void ClearDirty();
    bool IsDirty { get; }
    void WriteDelta(NetDataWriter writer);
    void ReadDelta(NetDataReader reader);
    void SkipDelta(NetDataReader reader);
}