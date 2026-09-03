using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.ConflictResolution;

/// <exclude />
public interface IChangeTrackingStore
{
    ref T GetChangeComponent<T>(int id)
        where T : struct, IComponent;
}