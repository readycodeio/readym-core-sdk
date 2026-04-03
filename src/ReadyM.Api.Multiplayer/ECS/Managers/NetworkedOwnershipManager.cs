using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Managers;

public class NetworkedOwnershipManager(Store store, ILogger logger)
{
    private readonly ComponentIndex<MetadataComponent, NetworkId> _ix = store.ComponentIndex<MetadataComponent, NetworkId>();
    
    public bool TryGetOwner(NetworkId netId, out PlayerId ownerId)
    {
        var matching = _ix[netId];
        
        switch (matching.Count)
        {
            case 0:
                ownerId = default;
                return false;
            case 1:
                ownerId = matching[0].GetComponent<MetadataComponent>().Owner;
                return true;
            default:
                logger.LogError("Multiple entities found with NetworkId {NetworkId}. This should not happen.", netId);
                ownerId = default;
                return false;
        }
    }
    
    public bool TryGetOwner(Entity entity, out PlayerId ownerId)
    {
        if (!entity.TryGetComponent<MetadataComponent>(out var meta))
        {
            ownerId = default;
            return false;
        }

        ownerId = meta.Owner;
        return true;
    }
    
    public bool TryGetOwner(MetadataComponent meta, out PlayerId ownerId)
    {
        ownerId = meta.Owner;
        return true;
    }
}