using System;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer;

public interface INetworkedComponent : IComponent
{
    void ClearDirty();
    bool IsDirty { get; }
}

public interface INetworkedEntityManager
{
    (Entity Entity, NetworkIdComponent NetId) CreateNetworkedEntity(ArchetypeId archetypeId);
    Entity CreateRemoteNetworkedEntity(ArchetypeId archetypeId, NetworkIdComponent netId);
    bool TryGetEntityByNetworkId(NetworkIdComponent netId, [NotNullWhen(true)] out Entity? entity);
    event Action<NetworkIdComponent>? onEntityDestroyed;
}