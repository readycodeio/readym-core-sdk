using System;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer;

public interface INetworkedComponent : IComponent
{
    void ClearDirty();
    bool IsDirty { get; }
}

public interface INetworkedArchetypeConfiguration
{
    INetworkedArchetypeConfiguration MarkSynced<T>() where T : struct, INetworkedComponent;
}

public interface INetworkedEntityManager
{
    void ConfigureArchetype(ArchetypeId archetypeId, Action<INetworkedArchetypeConfiguration> builder);
    (Entity Entity, NetworkIdComponent NetId) CreateNetworkedEntity(ArchetypeId archetypeId);
    Entity CreateRemoteNetworkedEntity(ArchetypeId archetypeId, NetworkIdComponent netId);
    bool TryGetEntityByNetworkId(NetworkIdComponent netId, [NotNullWhen(true)] out Entity? entity);
    event Action<NetworkIdComponent>? onEntityDestroyed;
}