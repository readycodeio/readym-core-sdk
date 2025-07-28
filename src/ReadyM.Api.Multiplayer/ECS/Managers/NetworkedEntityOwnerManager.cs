using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Managers;

public class NetworkedEntityOwnerManager(Store store, ILogger logger)
{
    public bool TryGetOwner(NetworkIdComponent netId, out PlayerId ownerId)
    {
        // FIXME: Shouldn't this be cached?
        var ix = store.ComponentIndex<MetadataComponent, NetworkIdComponent>();
        var matching = ix[netId];
        
        switch (matching.Count)
        {
            case 0:
                ownerId = default;
                return false;
            case 1:
                ownerId = matching[0].GetComponent<MetadataComponent>().Owner;
                return true;
            default:
                logger.LogError("Multiple entities found with NetworkIdComponent {NetworkId}. This should not happen.", netId);
                ownerId = default;
                return false;
        }
    }    
}